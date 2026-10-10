"""Publication-style PDF pages and repeat-level summaries for scene trials."""
from __future__ import annotations

from collections import defaultdict
from datetime import datetime
import io
import math
from pathlib import Path
import re
from xml.sax.saxutils import escape

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import Paragraph
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas

BLUE, ORANGE = "#2563A6", "#D87336"
INK, MUTED, PAPER, RULE = "#172D40", "#5C6F7B", "#F2F6F8", "#DCE5EB"
NAMES = {"colosseum": "Colosseum", "ragdolls": "Rotating cube + ragdolls"}
DESCRIPTIONS = {
    "colosseum": "21 concentric wall/platform rings over six tiers. Dense stacks and contact solving.",
    "ragdolls": "500 articulated ragdolls in a rotating container. Joint constraints under repeated collisions.",
}
plt.rcParams.update({"font.family": "DejaVu Sans", "font.size": 8, "axes.spines.top": False,
                     "axes.spines.right": False, "axes.labelcolor": INK, "text.color": INK,
                     "xtick.color": MUTED, "ytick.color": MUTED, "axes.edgecolor": RULE,
                     "grid.color": RULE, "grid.alpha": 0.6, "figure.facecolor": "white"})
FONT_DIR = Path(matplotlib.get_data_path()) / "fonts/ttf"
pdfmetrics.registerFont(TTFont("DejaVu", str(FONT_DIR / "DejaVuSans.ttf")))
pdfmetrics.registerFont(TTFont("DejaVu-Bold", str(FONT_DIR / "DejaVuSans-Bold.ttf")))


def group_trials(trials):
    groups = defaultdict(list)
    for trial in trials:
        groups[(trial["scene"], trial["threads"], trial["version"])].append(trial)
    return groups


def change(candidate, baseline):
    return (candidate / baseline - 1) * 100 if baseline > 0 else None


def summarize(manifest, trials):
    rows = []
    groups = group_trials(trials)
    for thread in manifest["settings"]["threads"]:
        for scene in manifest["settings"]["scenes"]:
            row = {"scene": scene, "threads": thread, "versions": {}}
            for version in ("baseline", "candidate"):
                runs = groups[(scene, thread, version)]
                means = [float(np.mean(t["stepMs"])) for t in runs]
                solve = [float(np.mean(t["stagesMs"]["Solve"])) for t in runs]
                row["versions"][version] = {
                    "meanMs": float(np.mean(means)), "runMeanMs": means,
                    "runRangeMs": [min(means), max(means)], "solveMs": float(np.mean(solve)),
                    "runSolveMs": solve, "p95Ms": float(np.mean([np.percentile(t["stepMs"], 95) for t in runs])),
                    "p99Ms": float(np.mean([np.percentile(t["stepMs"], 99) for t in runs])),
                    "totalStepSeconds": float(np.mean([sum(t["stepMs"]) / 1000 for t in runs])),
                    "realtimeFactor": manifest["settings"]["seconds"] / (np.mean(means) * manifest["settings"]["steps"] / 1000),
                    "meanContacts": float(np.mean([np.mean(t["contacts"]) for t in runs])),
                    "meanCandidatePairs": float(np.mean([np.mean(t["candidatePairs"]) for t in runs])),
                    "allocatedBytesPerStep": float(np.mean([t["allocatedBytes"] / t["measuredSteps"] for t in runs])),
                    "peakEngineMiB": float(np.mean([max(t["unmanagedBytes"]) / 2**20 for t in runs])),
                    "peakProcessMiB": float(np.mean([t["peakWorkingSetBytes"] / 2**20 for t in runs])),
                    "gcCollections": [sum(t["gcCollections"][i] for t in runs) for i in range(3)],
                    "stagesMs": {name: float(np.mean([np.mean(t["stagesMs"][name]) for t in runs]))
                                 for name in runs[0]["stagesMs"]},
                    "treeStagesMs": {name: float(np.mean([np.mean(t["treeStagesMs"][name]) for t in runs]))
                                     for name in runs[0]["treeStagesMs"]},
                    "peakAnchorRms": float(np.mean([max(t["anchorRms"]) for t in runs])),
                    "peakAnchorMax": float(np.mean([max(t["anchorMax"]) for t in runs])),
                }
            b, c = row["versions"]["baseline"], row["versions"]["candidate"]
            row["stepChangePercent"] = change(c["meanMs"], b["meanMs"])
            for stats in row["versions"].values():
                stats["outsideBucketsMs"] = max(0.0, stats["meanMs"] - sum(stats["stagesMs"].values()))
            row["solveChangePercent"] = change(c["solveMs"], b["solveMs"])
            row["contactChangePercent"] = change(c["meanContacts"], b["meanContacts"])
            rows.append(row)
    ratios = [r["versions"]["baseline"]["meanMs"] / r["versions"]["candidate"]["meanMs"] for r in rows]
    speedup = math.exp(np.mean(np.log(ratios)))
    return {"schemaVersion": 1, "rows": rows, "geometricMeanSpeedup": speedup,
            "overallComparison": overall_comparison(speedup)}


def overall_comparison(candidate_speedup):
    if math.isclose(candidate_speedup, 1, rel_tol=1e-12):
        return {"winner": None, "speedup": 1.0, "timeReductionPercent": 0.0}
    winner = "candidate" if candidate_speedup > 1 else "baseline"
    speedup = candidate_speedup if candidate_speedup > 1 else 1 / candidate_speedup
    return {"winner": winner, "speedup": speedup, "timeReductionPercent": (1 - 1 / speedup) * 100}


def friendly(name):
    return re.sub(r"(?<=[a-z])(?=[A-Z])", " ", name)


def pct(value):
    return "n/a" if value is None else f"{value:+.1f}%"


class Report:
    width, height = A4
    margin = 46

    def __init__(self, path, manifest):
        path.parent.mkdir(parents=True, exist_ok=True)
        self.pdf = canvas.Canvas(str(path), pagesize=A4, pageCompression=1)
        self.pdf.setTitle("Jitter Physics - Scene benchmark comparison")
        self.pdf.setAuthor("Jitter SceneBenchmark")
        self.manifest, self.page = manifest, 0
        self.content_width = self.width - 2 * self.margin
        self.style = ParagraphStyle("body", fontName="DejaVu", fontSize=9, leading=13, textColor=colors.HexColor(MUTED))

    def text(self, text, x, y, size=9, color=INK, bold=False):
        self.pdf.setFillColor(colors.HexColor(color))
        self.pdf.setFont("DejaVu-Bold" if bold else "DejaVu", size)
        self.pdf.drawString(x, y, str(text))

    def paragraph(self, text, y, width=None, x=None, size=9):
        style = ParagraphStyle("local", parent=self.style, fontSize=size, leading=size * 1.45)
        item = Paragraph(text, style)
        _, h = item.wrap(width or self.content_width, self.height)
        item.drawOn(self.pdf, x or self.margin, y - h)
        return y - h

    def start(self, eyebrow, title, subtitle=""):
        self.page += 1
        self.pdf.setFillColor(colors.HexColor(INK))
        self.pdf.rect(0, self.height - 12, self.width, 12, fill=1, stroke=0)
        self.text("JITTER / PERFORMANCE", self.margin, self.height - 43, 9, BLUE, True)
        self.text(eyebrow.upper(), self.margin, self.height - 63, 8, MUTED)
        self.text(title, self.margin, self.height - 96, 24, INK, True)
        if subtitle:
            self.paragraph(escape(subtitle), self.height - 111)
        self.pdf.setStrokeColor(colors.HexColor(RULE))
        self.pdf.line(self.margin, 39, self.width - self.margin, 39)
        self.text("Jitter Physics 2  |  Fixed scenes, isolated builds", self.margin, 25, 7.5, MUTED)
        self.pdf.drawRightString(self.width - self.margin, 25, f"{self.page:02d}")

    def finish(self):
        self.pdf.showPage()

    def figure(self, fig, top, height):
        stream = io.BytesIO()
        fig.savefig(stream, format="png", dpi=190, facecolor="white")
        plt.close(fig)
        stream.seek(0)
        self.pdf.drawImage(ImageReader(stream), self.margin, top - height,
                           width=self.content_width, height=height)

    def table(self, rows, widths, top, row_height=23, size=8):
        for i, row in enumerate(rows):
            y = top - (i + 1) * row_height
            if i == 0 or i % 2 == 0:
                self.pdf.setFillColor(colors.HexColor(INK if i == 0 else PAPER))
                self.pdf.rect(self.margin, y, sum(widths), row_height, stroke=0, fill=1)
            x = self.margin
            for text, width in zip(row, widths):
                self.text(text, x + 7, y + (row_height - size) / 2 + 1, size,
                          "#FFFFFF" if i == 0 else INK, i == 0)
                x += width
        return top - len(rows) * row_height

    def legend(self, y):
        for x, label, color in ((self.margin, "A / Baseline", BLUE), (self.margin + 132, "B / Candidate", ORANGE)):
            self.pdf.setFillColor(colors.HexColor(color))
            self.pdf.roundRect(x, y - 2, 15, 5, 2, fill=1, stroke=0)
            self.text(label, x + 23, y - 2, 8, color, True)


def binned(runs, values, settings):
    window = max(1, round(0.25 / settings["dt"]))
    slices = [slice(i, min(i + window, settings["steps"])) for i in range(0, settings["steps"], window)]
    x = np.array([(s.start + s.stop) / 2 * settings["dt"] for s in slices])
    data = np.array([[np.mean(values(t)[s]) for s in slices] for t in runs])
    return x, np.mean(data, axis=0), np.min(data, axis=0), np.max(data, axis=0)


def curve(ax, runs, values, settings, color, label, scale=1, linestyle="-"):
    x, mean, low, high = binned(runs, values, settings)
    ax.plot(x, mean * scale, color=color, label=label, lw=1.4, ls=linestyle,
            marker="o" if len(x) == 1 else None, markersize=3)
    if len(runs) > 1:
        ax.fill_between(x, low * scale, high * scale, color=color, alpha=0.13)
    ax.set_xlim(0, settings["seconds"])
    ax.grid(axis="y")


def overview(report, manifest, summary):
    settings = manifest["settings"]
    report.start("Version comparison", "Scenes under pressure", "A reproducible view of collision detection, contact solving, and articulated constraints.")
    versions = manifest["versions"]
    for y, label, key, color in ((664, "A / BASELINE", "baseline", BLUE), (607, "B / CANDIDATE", "candidate", ORANGE)):
        report.text(label, report.margin, y, 9, color, True)
        spec = versions[key]
        requested = spec['requested']
        if len(requested) > 28:
            requested = requested[:25] + "..."
        report.text(f"{requested}  /  {spec['commit'][:12]}", report.margin, y - 20, 16, INK, True)
    report.legend(555)
    comparison = summary["overallComparison"]
    winner = comparison["winner"]
    color = ORANGE if winner == "candidate" else BLUE if winner == "baseline" else INK
    headline = ("B / Candidate is faster overall" if winner == "candidate" else
                "A / Baseline is faster overall" if winner == "baseline" else "A and B are equal overall")
    report.text(f"{comparison['speedup']:.3f}x", report.margin, 504, 38, color, True)
    report.text(headline, report.margin + 210, 521, 13, color, True)
    report.text(f"{comparison['timeReductionPercent']:.2f}% lower step time" if winner else "Equal measured mean step time",
                report.margin + 210, 502, 9, MUTED)
    report.paragraph("Overall result uses the geometric mean of mean-step-time ratios across the selected scenes and "
                     "thread configurations. Each configuration has equal weight. See repeat ranges for measurement variability.", 483)
    report.text("THE EXPERIMENT", report.margin, 414, 9, BLUE, True)
    report.table([
        ["Simulation", "Execution", "Solver"],
        [f"{settings['seconds']:g} s per scene", f"{settings['repetitions']} fresh processes per version", f"{settings['iterations']} solve / {settings['relaxations']} relax"],
        [f"{settings['dt']:g} s fixed step", "Threads: " + ", ".join(map(str, settings['threads'])), f"{settings['substeps']} substep(s), single precision"],
        [f"{settings['warmupSeconds']:g} s discarded warm-up", "Sleeping disabled", "Release / .NET 10"],
    ], [155, 185, report.content_width - 340], 398, size=8)
    y = 280
    for scene in settings["scenes"]:
        report.text(NAMES[scene], report.margin, y, 13, INK, True)
        y = report.paragraph(DESCRIPTIONS[scene], y - 10) - 26
    report.paragraph("Read the final scorecard for absolute costs and relative changes. Use scene pages to explain "
                     "where the time went and whether the workload or joint quality changed. "
                     "Timing differences describe the evolving simulation on this machine.", 94, size=8)
    report.finish()


def methodology(report, manifest, trials):
    settings, machine = manifest["settings"], manifest["machine"]
    report.start("Provenance + method", "What was measured", "Recorded source snapshots and a fixed harness make the comparison reviewable and repeatable.")
    cpu_line = next((line.split(":", 1)[1].strip() for line in machine['cpu'].splitlines()
                     if line.startswith("Model name:")), machine["cpu"][:65])
    runtime = trials[0]["runtime"]
    y = report.table([
        ["Host", "Recorded configuration"],
        ["CPU", cpu_line[:67]], ["OS", machine["platform"][:67]],
        ["Runtime", runtime + " / " + ("server GC" if trials[0]["serverGC"] else "workstation GC")],
        ["Logical CPUs / affinity", f"{machine['logicalCpus']} / " + (str(len(machine['affinity'])) if machine['affinity'] else "unavailable")],
        ["Created (UTC)", datetime.fromisoformat(manifest["createdUtc"]).strftime("%Y-%m-%d %H:%M")],
    ], [130, report.content_width - 130], 670, row_height=22, size=8)
    y -= 25
    for key, label in (("baseline", "A / Baseline"), ("candidate", "B / Candidate")):
        spec = manifest["versions"][key]
        report.text(label + " source snapshot", report.margin, y, 10, INK, True)
        y = report.paragraph(f"Commit: {escape(spec['commit'])}<br/>SHA-256: {spec['sourceSha256']}", y - 10, size=7.5) - 17
    for title, body in [
        ("Timing boundaries", "The external stopwatch surrounds World.Step only. Container velocity updates, scene creation, "
         "observations, and JSON output sit outside it. Solve is Jitter's full Solve stage, including force and velocity integration, "
         "contacts, and constraints. Tree timings are nested within BroadPhase and must not be added to World timings."),
        ("Warm-up and repetition", f"Each process first runs a discarded {settings['warmupSeconds']:g}-second copy of its scene, then creates "
         f"a fresh scene for the measured 0-{settings['seconds']:g} second interval. Tiered compilation is disabled. "
         "A/B order alternates by scene and repeat; scenes rotate between repeats. Trials execute serially."),
        ("Reading the figures", "Curves show 0.25-second bin means, with the range of repeats shaded. Summaries average process means "
         "equally. Error bars show the minimum and maximum process mean, not confidence intervals. "
         "P95/P99 are computed within each run, then averaged. Frames in a simulation are correlated."),
        ("Workload and validity", "Both builds use the same frozen scene code, parameters, and initial placement. Sleeping is disabled. "
         "Different solvers can produce different trajectories and contact counts. Final positions, orientations, and velocities must "
         "be finite; body counts must remain constant. Ragdoll anchor separation is sampled every ten steps outside the stopwatch."),
    ]:
        report.text(title, report.margin, y, 10, BLUE, True)
        y = report.paragraph(body, y - 10, size=8) - 19
    local_status = manifest["versions"]["candidate"]["status"]
    selection = manifest.get("baselineSelection")
    if selection:
        report.paragraph("Baseline selection: " + escape(selection["description"]), 150, size=7.5)
    report.paragraph("Raw frames, complete environment overrides, full build hashes, process logs, and execution order are saved beside "
                     "the PDF in manifest.json and raw/. " + ("The local library includes uncommitted/untracked source; its content hash is recorded." if local_status else ""),
                     min(y, 88), size=7.5)
    report.finish()


def scene_timeline(report, manifest, groups, row):
    scene, thread = row["scene"], row["threads"]
    settings = manifest["settings"]
    report.start(f"Scene / {thread} total thread(s)", NAMES[scene], DESCRIPTIONS[scene])
    b, c = row["versions"]["baseline"], row["versions"]["candidate"]
    report.text(f"Mean step: {b['meanMs']:.3f} -> {c['meanMs']:.3f} ms  ({pct(row['stepChangePercent'])})", report.margin, 670, 11, INK, True)
    report.legend(647)
    fig, axes = plt.subplots(4, 1, figsize=(7.0, 7.0), sharex=True)
    for version, color, label in (("baseline", BLUE, "A"), ("candidate", ORANGE, "B")):
        runs = groups[(scene, thread, version)]
        curve(axes[0], runs, lambda t: t["stepMs"], settings, color, label)
        curve(axes[1], runs, lambda t: t["stagesMs"]["Solve"], settings, color, label)
        for stage, linestyle in (("NarrowPhase", "-"), ("BroadPhase", "--")):
            curve(axes[2], runs, lambda t, s=stage: t["stagesMs"][s], settings, color, f"{label} {friendly(stage)}", linestyle=linestyle)
        curve(axes[3], runs, lambda t: t["contacts"], settings, color, label, scale=0.001)
    for ax, title, ylabel in zip(axes, ("Total World.Step", "Solve stage", "Collision stages", "Active contact manifolds"),
                                ("ms / step", "ms / step", "ms / step", "thousands")):
        ax.set_title(title, loc="left", fontweight="bold", fontsize=9, pad=5)
        ax.set_ylabel(ylabel)
        ax.set_ylim(bottom=0)
    axes[2].legend(ncol=2, fontsize=6.5, loc="lower right", bbox_to_anchor=(1, 1), frameon=False)
    axes[-1].set_xlabel("Simulated time (seconds)")
    fig.subplots_adjust(left=0.12, right=0.98, top=0.97, bottom=0.075, hspace=0.48)
    report.figure(fig, 628, 505)
    report.paragraph(f"Solve change: {pct(row['solveChangePercent'])}. Mean contact workload: "
                     f"{b['meanContacts']:,.0f} -> {c['meanContacts']:,.0f} ({pct(row['contactChangePercent'])}). "
                     "Positive timing changes mean slower. The shaded band is repeat range; raw step spikes remain in the saved data.", 110, size=8)
    report.finish()


def timing_bars(ax, b, c, title):
    names = list(dict.fromkeys([*b, *c]))
    y = np.arange(len(names))
    maximum = max([*b.values(), *c.values(), 0.001])
    for offset, values, color, label in ((-0.17, b, BLUE, "A"), (0.17, c, ORANGE, "B")):
        array = np.array([values.get(n, np.nan) for n in names])
        ax.barh(y + offset, array, height=0.30, color=color, label=label)
        for index, value in enumerate(array):
            if not np.isfinite(value):
                ax.text(maximum * 0.015, index + offset, "n/a", va="center", fontsize=5.7, color=color)
                continue
            ax.text(value + maximum * 0.015, index + offset, f"{value:.3f}" if value >= 0.001 else f"{value:.4f}",
                    va="center", fontsize=5.7, color=color)
    ax.set_yticks(y, [friendly(n) for n in names], fontsize=6.6)
    ax.invert_yaxis()
    ax.set_xlim(0, maximum * 1.32)
    ax.set_xlabel("Mean ms / step", fontsize=7)
    ax.set_title(title, loc="left", fontsize=9, fontweight="bold")
    ax.grid(axis="x")
    ax.set_axisbelow(True)


def scene_detail(report, manifest, groups, row):
    scene, thread = row["scene"], row["threads"]
    b, c = row["versions"]["baseline"], row["versions"]["candidate"]
    runs = groups[(scene, thread, "baseline")]
    info = runs[0]
    report.start(f"Stage breakdown / {thread} total thread(s)", "Inside the step", NAMES[scene])
    report.text(f"{info['bodyCount'] - 1:,} scene bodies / {info['constraintCount']:,} constraints / sleeping disabled", report.margin, 677, 10, INK, True)
    report.legend(653)
    fig, axes = plt.subplots(1, 2, figsize=(7.0, 3.45), gridspec_kw={"width_ratios": [1.15, 1]})
    timing_bars(axes[0], {**b["stagesMs"], "Step boundary": b["outsideBucketsMs"]},
                {**c["stagesMs"], "Step boundary": c["outsideBucketsMs"]}, "World timing buckets")
    timing_bars(axes[1], b["treeStagesMs"], c["treeStagesMs"], "Inside BroadPhase")
    fig.subplots_adjust(left=0.21, right=0.99, top=0.91, bottom=0.13, wspace=1.1)
    report.figure(fig, 638, 245)
    y = report.table([
        ["Per-run metric", "A / Baseline", "B / Candidate"],
        ["P95 / P99 step (ms)", f"{b['p95Ms']:.3f} / {b['p99Ms']:.3f}", f"{c['p95Ms']:.3f} / {c['p99Ms']:.3f}"],
        ["Mean step range across repeats (ms)", f"{min(b['runMeanMs']):.3f} - {max(b['runMeanMs']):.3f}", f"{min(c['runMeanMs']):.3f} - {max(c['runMeanMs']):.3f}"],
        ["Managed allocation (KiB / step)", f"{b['allocatedBytesPerStep'] / 1024:.2f}", f"{c['allocatedBytesPerStep'] / 1024:.2f}"],
        ["GC collections (sum of repeats, G0/G1/G2)", "/".join(map(str, b['gcCollections'])), "/".join(map(str, c['gcCollections']))],
        ["Peak engine buffer memory (MiB)", f"{b['peakEngineMiB']:.1f}", f"{c['peakEngineMiB']:.1f}"],
        ["Mean broadphase candidate pairs", f"{b['meanCandidatePairs']:,.0f}", f"{c['meanCandidatePairs']:,.0f}"],
        ["Step time outside World timing buckets (ms)", f"{b['outsideBucketsMs']:.4f}", f"{c['outsideBucketsMs']:.4f}"],
    ], [255, 124, report.content_width - 379], 383, row_height=20, size=7.5)
    fig, ax = plt.subplots(figsize=(7.0, 1.65))
    for version, color, label in (("baseline", BLUE, "A"), ("candidate", ORANGE, "B")):
        group = groups[(scene, thread, version)]
        if scene == "ragdolls":
            curve(ax, group, lambda t: t["anchorRms"], manifest["settings"], color, label)
        else:
            curve(ax, group, lambda t: t["unmanagedBytes"], manifest["settings"], color, label, scale=1 / 2**20)
    ax.set_ylabel("Anchor RMS (units)" if scene == "ragdolls" else "Engine buffers (MiB)")
    ax.set_ylim(bottom=0)
    ax.set_xlabel("Simulated time (seconds)")
    fig.subplots_adjust(left=0.15, right=0.98, top=0.92, bottom=0.29)
    report.figure(fig, y - 10, 113)
    note = (f"Mean of peak anchor separation: RMS {b['peakAnchorRms']:.4f} -> {c['peakAnchorRms']:.4f}; "
            f"maximum {b['peakAnchorMax']:.4f} -> {c['peakAnchorMax']:.4f} units. "
            "This checks ball-socket separation, including hinges; it does not measure angular-limit error."
            if scene == "ragdolls" else "Engine memory covers body/contact/constraint buffers exposed by RawData, not total engine or process memory.")
    report.paragraph(note + " Tree buckets are nested measurements; n/a means a bucket is absent in that release. "
                     "All final body states passed the finite-state check.", 92, size=7.5)
    report.finish()


def scorecard(report, manifest, rows, thread):
    report.start(f"Final scorecard / {thread} total thread(s)", "Two versions, one scale", "Bars show absolute mean cost. Lower is faster; timing changes are candidate relative to baseline.")
    report.legend(675)
    fig, axes = plt.subplots(2, 1, figsize=(7.0, 5.4))
    x = np.arange(len(rows))
    for ax, metric, title, run_metric in ((axes[0], "meanMs", "Total World.Step", "runMeanMs"),
                                          (axes[1], "solveMs", "Solve stage", "runSolveMs")):
        for offset, version, color in ((-0.18, "baseline", BLUE), (0.18, "candidate", ORANGE)):
            means = [row["versions"][version][metric] for row in rows]
            lows = [max(0, mean - min(row["versions"][version][run_metric])) for mean, row in zip(means, rows)]
            highs = [max(0, max(row["versions"][version][run_metric]) - mean) for mean, row in zip(means, rows)]
            bars = ax.bar(x + offset, means, 0.34, color=color, yerr=[lows, highs], capsize=3,
                          error_kw={"elinewidth": 0.8, "ecolor": INK})
            for bar, mean, high in zip(bars, means, highs):
                ax.text(bar.get_x() + bar.get_width() / 2, mean + high, f"{mean:.2f}", ha="center", va="bottom", fontsize=8, color=color)
        ax.set_xticks(x, [NAMES[r["scene"]].replace(" + ", "\n+ ") for r in rows], fontsize=8)
        ax.set_ylabel("ms / step")
        ax.set_ylim(0, ax.get_ylim()[1] * 1.18)
        ax.set_title(title, loc="left", fontweight="bold", fontsize=11)
        ax.grid(axis="y")
        ax.set_axisbelow(True)
    fig.subplots_adjust(left=0.10, right=0.98, top=0.94, bottom=0.07, hspace=0.52)
    report.figure(fig, 653, 400)
    table_rows = [["Scene", "Step change", "Solve change", "Step elapsed A / B*"]]
    for row in rows:
        b, c = row["versions"]["baseline"], row["versions"]["candidate"]
        table_rows.append([NAMES[row["scene"]], pct(row["stepChangePercent"]), pct(row["solveChangePercent"]),
                           f"{b['totalStepSeconds']:.2f} / {c['totalStepSeconds']:.2f} s"])
    y = report.table(table_rows, [180, 88, 88, report.content_width - 356], 236, row_height=26, size=8)
    # These are elapsed step wall times, not summed worker CPU times.
    report.paragraph("*Accumulated elapsed time inside World.Step for one full simulated interval, averaged across repeats. "
                     "Error bars show the observed range of process means. Small differences within that range deserve repeated runs. "
                     "Speed and contact workload can change together; this is a scene benchmark.", y - 15, size=8)
    report.finish()


def create_report(manifest, trials, summary, destination):
    destination = Path(destination)
    temp = destination.with_suffix(destination.suffix + ".tmp")
    report = Report(temp, manifest)
    groups = group_trials(trials)
    overview(report, manifest, summary)
    methodology(report, manifest, trials)
    for row in summary["rows"]:
        scene_timeline(report, manifest, groups, row)
        scene_detail(report, manifest, groups, row)
    for thread in manifest["settings"]["threads"]:
        scorecard(report, manifest, [r for r in summary["rows"] if r["threads"] == thread], thread)
    report.pdf.save()
    temp.replace(destination)

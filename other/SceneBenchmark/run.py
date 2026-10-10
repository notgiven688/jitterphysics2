#!/usr/bin/env python3
"""Build two isolated Jitter revisions, run fixed scenes, and produce one PDF."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
import math
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tarfile
import tempfile

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
SCENES = ("colosseum", "ragdolls")
HARNESS_FILES = ("SceneBenchmark.csproj", "Program.cs", "Scenes.cs", "Colosseum.cs", "Ragdolls.cs")
RUNTIME_ENV = {"DOTNET_TieredCompilation": "0", "COMPlus_TieredCompilation": "0",
               "DOTNET_gcServer": "0", "COMPlus_gcServer": "0"}


def command(argv, *, cwd=REPO, env=None):
    return subprocess.check_output(argv, cwd=cwd, env=env, text=True, stderr=subprocess.STDOUT).strip()


def git(*args):
    return command(["git", *args])


def write_json(path, value):
    # Atomic manifests and summaries; trial files are created exclusively by C#.
    temp = path.with_suffix(path.suffix + ".tmp")
    temp.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    temp.replace(path)


def source_hash(root):
    digest = hashlib.sha256()
    for path in sorted(p for p in root.rglob("*") if p.is_file()):
        relative = path.relative_to(root).as_posix().encode()
        content = path.read_bytes()
        digest.update(len(relative).to_bytes(8, "big"))
        digest.update(relative)
        digest.update(len(content).to_bytes(8, "big"))
        digest.update(content)
    return digest.hexdigest()


def resolve_revision(value):
    if value == "local":
        return {"requested": value, "commit": git("rev-parse", "HEAD"),
                "status": git("status", "--porcelain", "--untracked-files=all", "--", "src/Jitter2")}
    # Verify once and use only the resolved commit for archive (also accepts HEAD^).
    commit = git("rev-parse", "--verify", "--end-of-options", value + "^{commit}")
    return {"requested": value, "commit": commit, "status": ""}


def snapshot(spec, destination):
    if spec["requested"] == "local":
        shutil.copytree(REPO / "src/Jitter2", destination,
                        ignore=shutil.ignore_patterns("bin", "obj", ".git", "__pycache__"))
    else:
        archive = subprocess.check_output(["git", "archive", spec["commit"] + ":src/Jitter2"], cwd=REPO)
        destination.mkdir(parents=True)
        with tarfile.open(fileobj=io.BytesIO(archive)) as source:
            source.extractall(destination, filter="data")
    spec["sourceSha256"] = source_hash(destination)


def build(spec, root, output, label, env):
    library = root / label / "Jitter2"
    snapshot(spec, library)
    harness = library.parent / "Harness"
    harness.mkdir()
    for name in HARNESS_FILES:
        shutil.copy2(HERE / name, harness / name)
    build_command = ["dotnet", "build", str(harness / "SceneBenchmark.csproj"), "-c", "Release",
                     "-p:DoublePrecision=false", f"-p:JitterProject={library / 'Jitter2.csproj'}",
                     "--disable-build-servers", "--nologo", "-v:q"]
    spec["buildCommand"] = build_command
    print(f"Building {label}: {spec['requested']} ({spec['commit'][:10]})", flush=True)
    with (output / f"build-{label}.log").open("w") as log:
        subprocess.run(build_command, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
    dll = harness / "bin/Release/net10.0/SceneBenchmark.dll"
    spec["assemblySha256"] = hashlib.sha256((dll.parent / "Jitter2.dll").read_bytes()).hexdigest()
    return dll


def make_schedule(scenes, threads, repetitions):
    schedule = []
    for repeat in range(repetitions):
        # Rotate scene order, reverse thread order, and alternate A/B within each pair.
        scene_order = list(scenes[repeat % len(scenes):]) + list(scenes[:repeat % len(scenes)])
        for thread in (threads if repeat % 2 == 0 else threads[::-1]):
            for index, scene in enumerate(scene_order):
                for label in (("baseline", "candidate") if (repeat + index) % 2 == 0
                              else ("candidate", "baseline")):
                    name = f"r{repeat + 1:02d}-t{thread:02d}-{scene}-{label}"
                    schedule.append({"repeat": repeat + 1, "threads": thread, "scene": scene,
                                     "version": label, "file": f"raw/{name}.json", "status": "pending"})
    return schedule


def load_dataset(directory):
    manifest = json.loads((directory / "manifest.json").read_text())
    if manifest.get("schemaVersion") != 1:
        raise ValueError("Unsupported manifest schema.")
    if manifest.get("status") != "complete":
        raise ValueError("The run is incomplete. Inspect manifest.json and trial logs; no partial report is published.")
    trials = []
    settings = manifest["settings"]
    if len(manifest["schedule"]) != 2 * len(settings["scenes"]) * len(settings["threads"]) * settings["repetitions"]:
        raise ValueError("The schedule does not contain the expected number of trials.")
    for entry in manifest["schedule"]:
        if (entry["status"] != "complete" or entry["scene"] not in settings["scenes"] or
                entry["threads"] not in settings["threads"] or entry["version"] not in ("baseline", "candidate")):
            raise ValueError("Unexpected or incomplete schedule entry.")
        path = (directory / entry["file"]).resolve()
        if not path.is_relative_to(directory.resolve()):
            raise ValueError("Trial file must be inside the result directory.")
        trial = json.loads(path.read_text())
        if trial.get("schemaVersion") != 1 or not trial.get("finiteFinalState"):
            raise ValueError(f"Invalid trial: {path.name}")
        for key in ("scene", "threads"):
            if trial[key] != entry[key]:
                raise ValueError(f"Trial {key} does not match the manifest: {path.name}")
        expected = {"measuredSteps": settings["steps"], "warmupSteps": settings["warmupSteps"],
                    "iterations": settings["iterations"], "relaxations": settings["relaxations"],
                    "substeps": settings["substeps"]}
        if any(trial[k] != value for k, value in expected.items()) or not math.isclose(trial["dt"], settings["dt"], rel_tol=1e-6):
            raise ValueError(f"Trial settings mismatch: {path.name}")
        series = [trial[k] for k in ("stepMs", "contacts", "activeBodies", "activeConstraints", "candidatePairs",
                                    "unmanagedBytes", "anchorRms", "anchorMax")]
        series.extend(trial["stagesMs"].values())
        series.extend(trial["treeStagesMs"].values())
        if "Solve" not in trial["stagesMs"] or any(len(v) != settings["steps"] for v in series):
            raise ValueError(f"Incomplete timing data: {path.name}")
        if any(not math.isfinite(x) or x < 0 for values in series for x in values):
            raise ValueError(f"Nonfinite or negative observation: {path.name}")
        if any(x <= 0 for x in trial["stepMs"]):
            raise ValueError(f"Nonpositive step time: {path.name}")
        trial["version"], trial["repeat"] = entry["version"], entry["repeat"]
        trials.append(trial)
    for thread in settings["threads"]:
        for scene in settings["scenes"]:
            for label in ("baseline", "candidate"):
                group = [t for t in trials if (t["scene"], t["threads"], t["version"]) == (scene, thread, label)]
                if len(group) != settings["repetitions"] or len({t["repeat"] for t in group}) != len(group):
                    raise ValueError(f"Missing or duplicated trials: {scene}, {thread}, {label}")
            pair = [t for t in trials if (t["scene"], t["threads"]) == (scene, thread)]
            if len({(t["bodyCount"], t["constraintCount"], t["anchorCount"]) for t in pair}) != 1:
                raise ValueError(f"Scene topology differs between builds: {scene}")
    return manifest, trials


def select_report_scenes(manifest, trials):
    # Saved runs can contain retired scenes; use only the current benchmark suite.
    scenes = [scene for scene in manifest["settings"]["scenes"] if scene in SCENES]
    if not scenes:
        raise ValueError("This saved run contains no scenes supported by the current benchmark suite.")
    selected = {**manifest, "settings": {**manifest["settings"], "scenes": scenes},
                "schedule": [entry for entry in manifest["schedule"] if entry["scene"] in scenes]}
    return selected, [trial for trial in trials if trial["scene"] in scenes]


def report(directory, output=None):
    from report import create_report, summarize
    manifest, trials = load_dataset(directory)
    manifest, trials = select_report_scenes(manifest, trials)
    destination = output or directory / "comparison.pdf"
    summary = summarize(manifest, trials)
    write_json(directory / "summary.json", summary)
    create_report(manifest, trials, summary, destination)
    print(f"PDF: {destination.resolve()}", flush=True)


def run(args):
    # Fail early for missing report dependencies, tools, refs, or invalid settings.
    import report as report_module  # noqa: F401
    for tool in ("git", "dotnet"):
        if not shutil.which(tool):
            raise ValueError(f"Missing {tool} on PATH.")
    versions = {"baseline": resolve_revision(args.baseline), "candidate": resolve_revision(args.candidate)}
    steps = round(args.seconds / args.dt)
    warmup_steps = round(args.warmup / args.dt)
    if steps < 1 or not math.isclose(steps * args.dt, args.seconds, abs_tol=1e-8):
        raise ValueError("--seconds must be a positive whole number of timesteps.")
    if not math.isclose(warmup_steps * args.dt, args.warmup, abs_tol=1e-8):
        raise ValueError("--warmup must be a whole number of timesteps.")
    output = (args.output or HERE / "results" / datetime.now().strftime("%Y%m%d-%H%M%S")).resolve()
    if output.exists() and any(output.iterdir()):
        raise ValueError(f"Refusing to reuse a nonempty output directory: {output}")
    output.mkdir(parents=True, exist_ok=True)
    (output / "raw").mkdir()
    env = dict(os.environ)
    env.update(RUNTIME_ENV)
    try:
        cpu = command(["lscpu"]) if shutil.which("lscpu") else platform.processor()
    except subprocess.CalledProcessError:
        cpu = platform.processor()
    manifest = {
        "schemaVersion": 1, "status": "building", "createdUtc": datetime.now(timezone.utc).isoformat(),
        "versions": versions, "harnessSha256": source_hash_of_harness(),
        "machine": {"platform": platform.platform(), "cpu": cpu, "logicalCpus": os.cpu_count(),
                    "affinity": sorted(os.sched_getaffinity(0)) if hasattr(os, "sched_getaffinity") else None,
                    "dotnet": command(["dotnet", "--info"]), "python": sys.version,
                    "runtimeEnvironment": {k: v for k, v in env.items() if k.startswith(("DOTNET_", "COMPlus_"))}},
        "settings": {"seconds": args.seconds, "dt": args.dt, "steps": steps,
                     "warmupSeconds": args.warmup, "warmupSteps": warmup_steps,
                     "repetitions": args.repetitions, "threads": args.threads, "scenes": args.scenes,
                     "iterations": args.iterations, "relaxations": args.relaxations, "substeps": args.substeps},
        "schedule": make_schedule(args.scenes, args.threads, args.repetitions),
    }
    path = output / "manifest.json"
    write_json(path, manifest)
    try:
        with tempfile.TemporaryDirectory(prefix="jitter-scene-bench-") as temp:
            root = Path(temp)
            executables = {label: build(spec, root, output, label, env) for label, spec in versions.items()}
            manifest["status"] = "running"
            write_json(path, manifest)
            schedule = manifest["schedule"]
            for index, entry in enumerate(schedule):
                print(f"[{index + 1}/{len(schedule)}] {entry['scene']} / {entry['threads']} threads / "
                      f"{entry['version']} / repeat {entry['repeat']}", flush=True)
                argv = ["dotnet", str(executables[entry["version"]]), entry["scene"], str(entry["threads"]),
                        str(warmup_steps), str(steps), str(args.dt), str(args.iterations), str(args.relaxations),
                        str(args.substeps), str(output / entry["file"])]
                entry["status"] = "running"
                write_json(path, manifest)
                with (output / entry["file"]).with_suffix(".log").open("w") as log:
                    subprocess.run(argv, env=env, stdout=log, stderr=subprocess.STDOUT,
                                   check=True, timeout=args.trial_timeout)
                entry["status"] = "complete"
                write_json(path, manifest)
                print((output / entry["file"]).with_suffix(".log").read_text().strip(), flush=True)
        manifest["status"] = "complete"
        write_json(path, manifest)
    except (Exception, KeyboardInterrupt) as error:
        manifest["status"], manifest["error"] = "failed", str(error)
        write_json(path, manifest)
        raise
    report(output)


def source_hash_of_harness():
    digest = hashlib.sha256()
    for name in HARNESS_FILES:
        digest.update(name.encode())
        digest.update((HERE / name).read_bytes())
    return digest.hexdigest()


def positive_int(value):
    number = int(value)
    if number < 1:
        raise argparse.ArgumentTypeError("must be at least 1")
    return number


def nonnegative_int(value):
    number = int(value)
    if number < 0:
        raise argparse.ArgumentTypeError("must be nonnegative")
    return number


def positive_float(value):
    number = float(value)
    if not math.isfinite(number) or number <= 0:
        raise argparse.ArgumentTypeError("must be finite and positive")
    return number


def nonnegative_float(value):
    number = float(value)
    if not math.isfinite(number) or number < 0:
        raise argparse.ArgumentTypeError("must be finite and nonnegative")
    return number


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    compare = sub.add_parser("compare", help="Build and benchmark two refs (tag, hash, HEAD^, or local).")
    compare.add_argument("baseline")
    compare.add_argument("candidate")
    compare.add_argument("--seconds", type=positive_float, default=20)
    compare.add_argument("--dt", type=positive_float, default=0.01)
    compare.add_argument("--warmup", type=nonnegative_float, default=2, help="Seconds in a discarded warm-up scene.")
    compare.add_argument("--repetitions", type=positive_int, default=3)
    compare.add_argument("--threads", type=positive_int, nargs="+", default=[1], help="Total threads, including caller.")
    compare.add_argument("--scenes", choices=SCENES, nargs="+", default=list(SCENES))
    compare.add_argument("--iterations", type=positive_int, default=8)
    compare.add_argument("--relaxations", type=nonnegative_int, default=4)
    compare.add_argument("--substeps", type=positive_int, default=1)
    compare.add_argument("--trial-timeout", type=positive_float, default=1800, help="Wall seconds allowed per process.")
    compare.add_argument("--output", type=Path, help="Fresh result directory (contains comparison.pdf).")
    render = sub.add_parser("report", help="Regenerate the PDF from completed raw trials without rerunning physics.")
    render.add_argument("directory", type=Path)
    render.add_argument("--output", type=Path, help="PDF file path.")
    args = parser.parse_args()
    try:
        if args.command == "compare":
            if args.dt > 1 / 60:
                raise ValueError("--dt must not exceed 1/60 s.")
            if len(set(args.threads)) != len(args.threads) or len(set(args.scenes)) != len(args.scenes):
                raise ValueError("Thread counts and scenes must not contain duplicates.")
            run(args)
        else:
            report(args.directory.resolve(), args.output)
    except (ValueError, OSError, subprocess.SubprocessError, ImportError) as error:
        parser.exit(1, f"Error: {error}\nSee result build/trial logs if a build or process failed.\n")


if __name__ == "__main__":
    main()

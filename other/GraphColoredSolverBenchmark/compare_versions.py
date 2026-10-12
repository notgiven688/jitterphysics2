#!/usr/bin/env python3
"""Compare prebuilt graph-colored solver variants in alternating fresh processes."""

import argparse
import csv
import hashlib
import io
import json
import platform
import statistics
import subprocess
import time
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--variant", action="append", required=True, metavar="NAME=DLL")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--warmup", type=int, default=300)
    parser.add_argument("--steps", type=int, default=600)
    parser.add_argument("--repeats", type=int, default=3)
    parser.add_argument("--threads", type=int, nargs="+", default=[1, 4])
    args = parser.parse_args()
    variants = [(name, Path(path).resolve()) for name, path in
                (value.split("=", 1) for value in args.variant)]
    if len({name for name, _ in variants}) != len(variants):
        parser.error("variant names must be unique")
    if min(args.warmup, args.steps, args.repeats, *args.threads) <= 0:
        parser.error("counts must be positive")
    args.output.mkdir(parents=True, exist_ok=True)

    def sha256(path):
        return hashlib.sha256(path.read_bytes()).hexdigest()

    metadata = {
        "platform": platform.platform(), "warmup_steps": args.warmup,
        "measured_steps": args.steps, "repeats": args.repeats,
        "threads": args.threads, "variants": {}, "processes": []}
    for name, dll in variants:
        metadata["variants"][name] = {
            "benchmark_dll": str(dll), "benchmark_sha256": sha256(dll),
            "jitter_sha256": sha256(dll.parent / "Jitter2.dll")}
    rows = []
    for repeat in range(1, args.repeats + 1):
        # Reverse the order on every other repeat to reduce ordering bias.
        order = variants if repeat % 2 else list(reversed(variants))
        for threads in args.threads:
            for name, dll in order:
                command = ["dotnet", str(dll), "graphcolored", "both", str(threads),
                           str(args.warmup), str(args.steps)]
                print(f"repeat={repeat} threads={threads} variant={name}", flush=True)
                start = time.monotonic()
                result = subprocess.run(command, capture_output=True, text=True, check=True)
                elapsed = time.monotonic() - start
                filename = f"repeat-{repeat}-{threads}-threads-{name}.csv"
                (args.output / filename).write_text(result.stdout)
                if result.stderr:
                    (args.output / (filename + ".stderr")).write_text(result.stderr)
                metadata["processes"].append({
                    "repeat": repeat, "variant": name, "threads": threads,
                    "elapsed_seconds": elapsed, "command": command,
                    "output": filename})
                data = "\n".join(line for line in result.stdout.splitlines()
                                 if not line.startswith("#"))
                for row in csv.DictReader(io.StringIO(data)):
                    rows.append({"variant": name, "repeat": repeat, **row})
                (args.output / "metadata.json").write_text(json.dumps(metadata, indent=2) + "\n")

    with (args.output / "runs.csv").open("w", newline="") as target:
        writer = csv.DictWriter(target, fieldnames=list(rows[0]), lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    summary = []
    for threads in args.threads:
        for scene in ("ground", "pile"):
            for name, _ in variants:
                group = [row for row in rows if row["variant"] == name
                         and int(row["threads"]) == threads and row["scene"] == scene]
                row = {"variant": name, "scene": scene, "threads": threads,
                       "runs": len(group)}
                for metric in ("step_ms", "solve_ms", "mean_contacts", "min_y", "max_y",
                               "allocated_bytes"):
                    values = [float(entry[metric]) for entry in group]
                    for label, reduction in (("median", statistics.median), ("min", min), ("max", max)):
                        row[f"{metric}_{label}"] = f"{reduction(values):.6f}"
                row["all_finite"] = all(entry["finite"] == "true" for entry in group)
                row["gc_collections"] = sum(int(entry[key]) for entry in group
                                            for key in ("gc0", "gc1", "gc2"))
                summary.append(row)
    with (args.output / "summary.csv").open("w", newline="") as target:
        writer = csv.DictWriter(target, fieldnames=list(summary[0]), lineterminator="\n")
        writer.writeheader()
        writer.writerows(summary)


if __name__ == "__main__":
    main()

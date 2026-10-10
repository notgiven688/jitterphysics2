"""Regression checks for measurement aggregation and report data integrity."""
import copy
import json
from pathlib import Path
import tempfile
import unittest

import run
from report import binned, curve, overall_comparison, summarize, timing_bars


def fixture():
    settings = {"threads": [1], "scenes": ["colosseum"], "repetitions": 2,
                "seconds": 0.02, "steps": 2, "dt": 0.01, "warmupSteps": 0,
                "iterations": 8, "relaxations": 4, "substeps": 1}
    schedule = run.make_schedule(settings["scenes"], settings["threads"], 2)
    trials = []
    for entry in schedule:
        value = entry["repeat"] * (2 if entry["version"] == "candidate" else 1)
        trial = {"schemaVersion": 1, "finiteFinalState": True, "scene": entry["scene"], "threads": 1,
                 "version": entry["version"], "repeat": entry["repeat"], "dt": 0.01, "measuredSteps": 2,
                 "warmupSteps": 0, "iterations": 8, "relaxations": 4, "substeps": 1,
                 "bodyCount": 10, "constraintCount": 0, "anchorCount": 0, "stepMs": [value, value],
                 "stagesMs": {"Solve": [value / 2, value / 2]}, "treeStagesMs": {"ScanOverlaps": [0.1, 0.1]},
                 "allocatedBytes": 100, "gcCollections": [0, 0, 0], "peakWorkingSetBytes": 2**20,
                 "contacts": [10, 20], "activeBodies": [10, 10], "activeConstraints": [0, 0],
                 "candidatePairs": [20, 40], "unmanagedBytes": [2**20, 2**20],
                 "anchorRms": [0, 0], "anchorMax": [0, 0]}
        entry["status"] = "complete"
        trials.append(trial)
    return {"schemaVersion": 1, "status": "complete", "settings": settings, "schedule": schedule}, trials


class AggregationTests(unittest.TestCase):
    def test_repeat_means_and_speedup_direction(self):
        manifest, trials = fixture()
        summary = summarize(manifest, trials)
        row = summary["rows"][0]
        self.assertEqual(row["versions"]["baseline"]["meanMs"], 1.5)
        self.assertEqual(row["versions"]["candidate"]["runRangeMs"], [2, 4])
        self.assertEqual(row["stepChangePercent"], 100)
        self.assertAlmostEqual(summary["geometricMeanSpeedup"], 0.5)
        self.assertEqual(row["versions"]["baseline"]["allocatedBytesPerStep"], 50)
        self.assertAlmostEqual(row["versions"]["baseline"]["totalStepSeconds"], 0.003)

    def test_geometric_mean_weights_configurations_equally(self):
        manifest, trials = fixture()
        manifest["settings"]["scenes"].append("ragdolls")
        extra = copy.deepcopy(trials)
        for trial in extra:
            trial["scene"] = "ragdolls"
            # Make ragdolls much slower in absolute terms, but candidate 2x faster.
            multiplier = 100 if trial["version"] == "baseline" else 25
            trial["stepMs"] = [x * multiplier for x in trial["stepMs"]]
        self.assertAlmostEqual(summarize(manifest, trials + extra)["geometricMeanSpeedup"], 1.0)

    def test_overall_result_identifies_either_winner_or_a_tie(self):
        for ratio, winner in ((2, "candidate"), (0.5, "baseline"), (1, None), (1 + 1e-14, None)):
            with self.subTest(ratio=ratio):
                comparison = overall_comparison(ratio)
                self.assertEqual(comparison["winner"], winner)
                self.assertEqual(comparison["speedup"], 2 if winner else 1)
                self.assertEqual(comparison["timeReductionPercent"], 50 if winner else 0)

    def test_binning_keeps_short_final_interval_and_repeat_range(self):
        settings = {"dt": 0.1, "steps": 3, "seconds": 0.3}
        x, mean, low, high = binned([[1, 3, 9], [3, 5, 11]], lambda t: t, settings)
        self.assertEqual(list(mean), [3, 10])
        self.assertEqual(list(low), [2, 9])
        self.assertEqual(list(high), [4, 11])
        self.assertAlmostEqual(x[-1], 0.25)

    def test_every_pair_uses_both_versions_and_changes_order(self):
        schedule = run.make_schedule(list(run.SCENES), [1, 16], 3)
        self.assertEqual(len(schedule), 24)
        pairs = [(schedule[i], schedule[i + 1]) for i in range(0, len(schedule), 2)]
        for first, second in pairs:
            self.assertEqual((first["scene"], first["threads"], first["repeat"]),
                             (second["scene"], second["threads"], second["repeat"]))
            self.assertNotEqual(first["version"], second["version"])
        self.assertEqual(schedule[0]["version"], "baseline")
        self.assertEqual(schedule[8]["version"], "candidate")
        self.assertEqual(schedule[8]["threads"], 16)

    def test_multiple_curves_share_unclipped_limits(self):
        import matplotlib.pyplot as plt
        fig, ax = plt.subplots()
        settings = {"dt": 0.01, "steps": 30, "seconds": 0.3}
        curve(ax, [[0.01] * 30], lambda t: t, settings, "blue", "narrow")
        curve(ax, [[0.6] * 30], lambda t: t, settings, "orange", "broad")
        ax.set_ylim(bottom=0)
        self.assertGreater(ax.get_ylim()[1], 0.6)
        plt.close(fig)

    def test_missing_historical_timing_bucket_is_labeled_unavailable(self):
        import math
        import matplotlib.pyplot as plt
        fig, ax = plt.subplots()
        timing_bars(ax, {"ScanOverlaps": 1}, {"ScanOverlaps": 2, "Optimize": 0.1}, "Tree")
        fig.canvas.draw()
        self.assertIn("n/a", [text.get_text() for text in ax.texts])
        self.assertTrue(all(math.isfinite(value) for text in ax.texts for value in text.get_position()))
        plt.close(fig)


class IntegrityTests(unittest.TestCase):
    def test_legacy_box_trials_are_excluded_from_the_report_and_aggregate(self):
        manifest, trials = fixture()
        retired = copy.deepcopy(trials)
        manifest["settings"]["scenes"].insert(0, "rotating-cube")
        for trial in retired:
            trial["scene"] = "rotating-cube"
            if trial["version"] == "candidate":
                trial["stepMs"] = [0.001, 0.001]
        selected, selected_trials = run.select_report_scenes(manifest, trials + retired)
        self.assertEqual(run.SCENES, ("colosseum", "ragdolls"))
        self.assertEqual(selected["settings"]["scenes"], ["colosseum"])
        self.assertEqual(manifest["settings"]["scenes"], ["rotating-cube", "colosseum"])
        summary = summarize(selected, selected_trials)
        self.assertAlmostEqual(summary["geometricMeanSpeedup"], 0.5)
        self.assertEqual(summary["overallComparison"]["winner"], "baseline")

    def test_saved_run_with_only_retired_scenes_has_no_report(self):
        manifest, trials = fixture()
        manifest["settings"]["scenes"] = ["rotating-cube"]
        with self.assertRaisesRegex(ValueError, "no scenes supported"):
            run.select_report_scenes(manifest, trials)

    def write_fixture(self, directory, manifest, trials):
        (directory / "raw").mkdir(exist_ok=True)
        (directory / "manifest.json").write_text(json.dumps(manifest))
        for entry, trial in zip(manifest["schedule"], trials):
            (directory / entry["file"]).write_text(json.dumps(trial))

    def test_complete_data_loads_and_preserves_repeat_labels(self):
        manifest, trials = fixture()
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.write_fixture(root, manifest, trials)
            _, loaded = run.load_dataset(root)
            self.assertEqual([t["repeat"] for t in loaded], [1, 1, 2, 2])

    def test_rejects_corruption_or_incomplete_experiment(self):
        for kind in ("missing-frame", "nonfinite", "topology", "settings", "failed", "duplicate", "outside"):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temp:
                manifest, trials = fixture()
                root = Path(temp)
                if kind == "missing-frame": trials[0]["stepMs"].pop()
                elif kind == "nonfinite": trials[0]["anchorRms"][0] = float("nan")
                elif kind == "topology": trials[0]["constraintCount"] = 1
                elif kind == "settings": trials[0]["dt"] = 0.02
                elif kind == "failed": manifest["status"] = "failed"
                elif kind == "duplicate": manifest["schedule"][2]["repeat"] = 1
                self.write_fixture(root, manifest, trials)
                if kind == "outside":
                    manifest["schedule"][0]["file"] = "../trial.json"
                    (root / "manifest.json").write_text(json.dumps(manifest))
                with self.assertRaises(ValueError): run.load_dataset(root)

    def test_content_hash_is_location_independent_and_includes_names(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            a, b = root / "a", root / "b"
            a.mkdir(); b.mkdir()
            (a / "x.cs").write_text("same source")
            (b / "x.cs").write_text("same source")
            self.assertEqual(run.source_hash(a), run.source_hash(b))
            (b / "x.cs").rename(b / "y.cs")
            self.assertNotEqual(run.source_hash(a), run.source_hash(b))


if __name__ == "__main__":
    unittest.main()

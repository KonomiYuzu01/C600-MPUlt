"""Black-box safety checks for team tooling, using disposable synthetic repositories only."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
POWERSHELL = shutil.which("pwsh") or shutil.which("powershell")


class TeamToolTests(unittest.TestCase):
    def setUp(self):
        work = (ROOT / "work").resolve()
        work.mkdir(exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(prefix="team-tools-test-", dir=work)
        self.root = Path(self.temporary.name).resolve()
        self.assertTrue(self.root.is_relative_to(work))
        self.addCleanup(self.temporary.cleanup)
        self.source = self.root / "source"
        (self.source / "assets").mkdir(parents=True)
        (self.source / "tests").mkdir()
        (self.root / "scripts").mkdir()
        shutil.copy2(ROOT / "scripts/verify_team.py", self.root / "scripts/verify_team.py")
        (self.root / "team-project.json").write_text(json.dumps({"source_root": "source"}), encoding="utf-8")
        (self.source / "core.py").write_text("# Synthetic fixture, not the application.\n", encoding="utf-8")
        (self.source / "assets/manifest.json").write_text('{"synthetic": true}\n', encoding="utf-8")
        for name, code in (("core", 23), ("reference_maps", 0), ("crash", 0)):
            (self.source / "tests" / ("test_" + name + ".py")).write_text(
                "from pathlib import Path\n"
                + "Path(" + repr(name + ".ran") + ").write_text('executed', encoding='utf-8')\n"
                + "print(" + repr("fixture " + name) + ", flush=True)\n"
                + "raise SystemExit(" + str(code) + ")\n", encoding="utf-8")
        self.git("init", "--quiet")
        self.git("add", ".")
        self.git("-c", "user.name=Team Tool Fixture", "-c", "user.email=fixture@example.invalid",
                 "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "Synthetic fixture")

    def git(self, *arguments):
        result = subprocess.run(["git", "-C", str(self.source), *arguments],
                                capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result.stdout.strip()

    def verify(self, *arguments):
        result = subprocess.run([sys.executable, str(self.root / "scripts/verify_team.py"),
                                 "--stage", "core", "--python", sys.executable, *arguments],
                                cwd=self.root, capture_output=True, text=True,
                                encoding="utf-8", errors="replace", timeout=60)
        reports = list((self.root / "work/team-evidence").glob("*/report.json"))
        self.assertEqual(len(reports), 1, result.stdout + result.stderr)
        report = json.loads(reports[0].read_text(encoding="utf-8"))
        self.assertEqual(report["exit_code"], result.returncode)
        self.assertEqual(report["source_root"], str(self.source))
        self.assertEqual(report["git_before"]["head"], self.git("rev-parse", "HEAD"))
        return result, report

    def test_requires_explicit_source_test_opt_in(self):
        result, report = self.verify()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("--allow-source-tests", report["error"])
        self.assertEqual(report["commands"], [])
        self.assertEqual(list(self.source.glob("*.ran")), [])

    def test_failure_code_and_log_preserved_and_later_commands_not_run(self):
        result, report = self.verify("--allow-source-tests")
        self.assertEqual(result.returncode, 23)
        self.assertEqual(report["status"], "failed")
        self.assertTrue((self.source / "core.ran").is_file())
        self.assertFalse((self.source / "reference_maps.ran").exists())
        self.assertFalse((self.source / "crash.ran").exists())
        self.assertEqual(len(report["commands"]), 1)
        command = report["commands"][0]
        self.assertEqual(command["exit_code"], 23)
        self.assertEqual(command["status"], "failed")
        self.assertIn("fixture core", Path(command["log"]).read_text(encoding="utf-8"))

    def test_dry_run_records_plan_without_execution(self):
        result, report = self.verify("--dry-run")
        self.assertEqual(result.returncode, 0)
        self.assertEqual(report["status"], "dry-run")
        self.assertEqual(len(report["commands"]), 3)
        self.assertTrue(all(item["status"] == "planned" and "exit_code" not in item for item in report["commands"]))
        self.assertEqual(list(self.source.glob("*.ran")), [])

    @unittest.skipUnless(os.name == "nt" and POWERSHELL, "Windows PowerShell worktree helper requires Windows")
    def test_worktree_clean_creation_then_dirty_refusal(self):
        script = ROOT / "scripts/new-team-worktree.ps1"
        destination = self.root / "clean-worktree"
        command = [POWERSHELL, "-NoProfile", "-NonInteractive", "-File", str(script),
                   "-SourceRoot", str(self.source), "-Destination", str(destination), "-BaseRef", "HEAD"]
        result = subprocess.run(command, capture_output=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertTrue((destination / "core.py").is_file())
        result = subprocess.run(["git", "-C", str(destination), "rev-parse", "HEAD"],
                                capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0)
        self.assertEqual(result.stdout.strip(), self.git("rev-parse", "HEAD"))
        (self.source / "core.py").write_text("# Unsaved owner changes must remain intact.\n", encoding="utf-8")
        rejected_destination = self.root / "must-not-exist"
        command[command.index("-Destination") + 1] = str(rejected_destination)
        result = subprocess.run(command, capture_output=True, timeout=60)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b"uncommitted changes", result.stdout + result.stderr)
        self.assertFalse(rejected_destination.exists())
        self.assertEqual((self.source / "core.py").read_text(encoding="utf-8"),
                         "# Unsaved owner changes must remain intact.\n")


if __name__ == "__main__":
    unittest.main(verbosity=2)

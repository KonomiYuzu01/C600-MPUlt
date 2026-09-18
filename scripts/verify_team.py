"""Explicit verification stages; application tests are opt-in and use a chosen checkout."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
from datetime import datetime, timezone
import uuid
import tomllib

ROOT = Path(__file__).resolve().parents[1]
STAGES = {
    "core": ["tests/test_core.py", "tests/test_reference_maps.py", "tests/test_crash.py"],
    "lifecycle": ["tests/test_engine_lifecycle.py", "tests/test_frame_preferences.py", "tests/test_packaged_engine_command.py"],
    "native-layout": ["native/bootstrap.py", "--self-test-only"],
    "native-auxiliary": ["tests/test_native_auxiliary_controls.py"],
    "native-renderer": ["native/bootstrap.py", "--renderer-test"],
}


def git_identity(source):
    result = {}
    for key, args in (("head", ["rev-parse", "HEAD"]), ("status", ["status", "--porcelain=v1"]), ("diff_sha256", ["diff", "HEAD", "--binary"])):
        proc = subprocess.run(["git", "-C", str(source), *args], capture_output=True)
        result[key] = hashlib.sha256(proc.stdout).hexdigest() if key == "diff_sha256" else proc.stdout.decode("utf-8", "replace").strip()
        result[key + "_exit_code"] = proc.returncode
    result["note"] = "Git identity excludes ignored experiments and untracked file contents; this is not a complete source snapshot."
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stage", choices=["infrastructure", *STAGES], default="infrastructure")
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--python", default=sys.executable, help="Application interpreter, ideally the source checkout's virtualenv Python")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--allow-source-tests", action="store_true", help="Acknowledge existing tests write reports inside the selected checkout")
    parser.add_argument("--allow-native", action="store_true", help="Confirm exclusive access to the Windows GUI for native fixture windows")
    args = parser.parse_args()
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + uuid.uuid4().hex[:8]
    evidence = ROOT / "work" / "team-evidence" / stamp
    evidence.mkdir(parents=True, exist_ok=False)
    report = {"stage": args.stage, "dry_run": args.dry_run, "started_utc": stamp, "commands": [], "lint": "not configured", "typecheck": "not configured", "status": "failed"}
    code = 1
    source = None
    try:
        config_path = ROOT / "team-project.json"
        config = json.loads(config_path.read_text(encoding="utf-8-sig")) if config_path.is_file() else {}
        value = args.source_root or config.get("source_root")
        if not value or not isinstance(value, (str, Path)):
            raise ValueError("Provide --source-root or a nonempty source_root in team-project.json")
        source = Path(value)
        if not source.is_absolute():
            source = ROOT / source
        source = source.resolve()
        if not (source / "core.py").is_file() or not (source / "assets/manifest.json").is_file():
            raise ValueError("Source root must be a Magic 600 Cell source checkout containing core.py and assets/manifest.json")
        report["source_root"] = str(source)
        report["git_before"] = git_identity(source)
        if report["git_before"]["head_exit_code"] != 0:
            raise ValueError("Cannot identify source Git HEAD")
        report["model_manifest_sha256"] = hashlib.sha256((source / "assets/manifest.json").read_bytes()).hexdigest()
        if args.stage == "infrastructure":
            for name in ("verify.ps1", "verify.sh", "verify_team.py"):
                path = ROOT / "scripts" / name
                if not path.is_file():
                    raise ValueError("Missing verification entrypoint: " + name)
            compile(Path(__file__).read_text(encoding="utf-8"), str(__file__), "exec")
            roles = ("staff-orchestrator", "architect", "planner", "implementer", "spec-reviewer", "code-reviewer", "verifier")
            for role in roles:
                skill = (ROOT / ".agents" / "skills" / role / "SKILL.md").read_text(encoding="utf-8-sig")
                if not skill.startswith("---\n") or "\n---\n" not in skill[4:]:
                    raise ValueError("Invalid skill frontmatter: " + role)
                metadata = skill.split("---", 2)[1]
                if "name: " + role not in metadata or "description: " not in metadata:
                    raise ValueError("Missing skill metadata: " + role)
                agent = tomllib.loads((ROOT / ".codex" / "agents" / (role + ".toml")).read_text(encoding="utf-8"))
                if agent.get("name") != role or not agent.get("description") or not agent.get("developer_instructions"):
                    raise ValueError("Invalid project agent definition: " + role)
            settings = tomllib.loads((ROOT / ".codex" / "config.toml").read_text(encoding="utf-8"))
            if settings.get("agents", {}).get("max_concurrent_threads_per_session") != 3:
                raise ValueError("Deployment expects three child slots plus the primary")
            for document in ("docs/team/OPERATING_MODEL.md", "docs/team/VERIFICATION.md"):
                if not (ROOT / document).is_file():
                    raise ValueError("Missing team document: " + document)
            report["checks"] = ["Source locator and model manifest available", "Git HEAD identified", "Verification entrypoints present", "Python runner syntax valid", "Seven skill and agent definitions structurally valid", "Project concurrency config valid", "Team operating and verification documents present"]
            report["scope"] = "Team infrastructure structure only; does not validate role execution, API access, application behavior, or GUI."
        else:
            native = args.stage.startswith("native-")
            if not args.dry_run and not args.allow_source_tests:
                raise ValueError("Use an isolated checkout and pass --allow-source-tests; existing tests write reports into their checkout")
            if native and not args.dry_run and (os.name != "nt" or not args.allow_native):
                raise ValueError("Native tests require Windows and --allow-native; coordinate exclusive GUI ownership first")
            entries = STAGES[args.stage]
            if native:
                command = [args.python, str(source / entries[0]), *entries[1:]]
                command += ["--output" if args.stage == "native-auxiliary" else "--data", str(evidence / "native-session")]
                commands = [command]
            else:
                commands = [[args.python, str(source / entry)] for entry in entries]
            for index, command in enumerate(commands, 1):
                test_path = Path(command[1])
                if not test_path.is_file():
                    raise ValueError("Missing test entrypoint: " + str(test_path))
                item = {"argv": command, "cwd": str(source), "entrypoint_sha256": hashlib.sha256(test_path.read_bytes()).hexdigest(), "status": "planned"}
                report["commands"].append(item)
                print(json.dumps(item, ensure_ascii=False), flush=True)
                if args.dry_run:
                    continue
                log = evidence / (str(index) + "-" + test_path.stem + ".log")
                item["log"] = str(log)
                with log.open("w", encoding="utf-8") as output:
                    proc = subprocess.run(command, cwd=source, stdout=output, stderr=subprocess.STDOUT)
                item["exit_code"] = proc.returncode
                item["status"] = "passed" if proc.returncode == 0 else "failed"
                print("Exit code:", proc.returncode, "Log:", log, flush=True)
                if proc.returncode:
                    code = proc.returncode
                    raise RuntimeError("Verification command failed; remaining commands were not run")
        report["status"] = "dry-run" if args.dry_run else "passed"
        code = 0
    except (OSError, ValueError, RuntimeError) as error:
        report["error"] = str(error)
        print(str(error), file=sys.stderr)
    finally:
        if source is not None and source.is_dir():
            try:
                report["git_after"] = git_identity(source)
            except OSError as error:
                report["git_after_error"] = str(error)
        report["exit_code"] = code
        report["finished_utc"] = datetime.now(timezone.utc).isoformat()
        target = evidence / "report.json"
        target.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
        print("Evidence:", target)
    return code


if __name__ == "__main__":
    raise SystemExit(main())

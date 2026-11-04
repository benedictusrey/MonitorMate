"""Report a three-package release plan without building or changing the machine."""

from __future__ import annotations

import argparse
import json
import xml.etree.ElementTree as ET
from pathlib import Path


def portable_project_errors(project: Path) -> list[str]:
    """Reject missing hosts, Windows target frameworks and WPF/Windows Forms."""
    if not project.is_file():
        return ["The portable desktop project does not exist; implement the port first."]
    try:
        tree = ET.parse(project)
    except ET.ParseError:
        return ["The project XML is invalid."]
    elements = [(node.tag.rsplit("}", 1)[-1], (node.text or "").strip())
                for node in tree.getroot().iter()]
    frameworks = [text for tag, text in elements if tag in {"TargetFramework", "TargetFrameworks"}]
    errors = []
    if not frameworks or any("-windows" in framework.lower() for framework in frameworks):
        errors.append("A platform-neutral target framework is required.")
    if any(tag in {"UseWPF", "UseWindowsForms"} and text.lower() == "true" for tag, text in elements):
        errors.append("WPF/Windows Forms dependencies prevent portable publishing.")
    return errors


def build_plan(root: Path) -> dict[str, object]:
    """Return four proposed publishes and three package targets with honest blockers."""
    definition = json.loads((root / "packaging/desktop-targets.json").read_text(encoding="utf-8"))
    errors = portable_project_errors(root / definition["desktop_project"])
    commands = [["dotnet", "publish", definition["current_windows_project"] if runtime == "win-x64" else definition["desktop_project"], "-c", "Release", "-r", runtime,
                 "--self-contained", "true", "-p:UseAppHost=true", "-p:PublishSingleFile=false",
                 "-o", f"artifacts/{runtime}"] for runtime in definition["runtimes"]]
    return {"status": "blocked" if errors else "project precheck passed; native backends and package tooling still required",
            "blockers": errors, "publish_commands_after_port": commands,
            "primary_packages": definition["packages"], "installers_created": 0,
            "native_behavior_validated": False}


def main() -> int:
    """Print a read-only plan; nonzero means the portable project is not ready."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    result = build_plan(Path(__file__).resolve().parents[1])
    print(json.dumps(result, indent=2))
    return int(bool(result["blockers"]))


if __name__ == "__main__":
    raise SystemExit(main())

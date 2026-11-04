"""Regression checks for rejecting an invalid or Windows-only portable host."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from plan_desktop_release import build_plan, portable_project_errors


class PlatformPlanTests(unittest.TestCase):
    """Test actual project boundaries without running builds or native code."""

    def test_current_wpf_project_is_rejected(self) -> None:
        root = Path(__file__).resolve().parents[1]
        errors = portable_project_errors(root / "src/MonitorMate/MonitorMate.csproj")
        self.assertEqual(len(errors), 2)

    def test_missing_port_does_not_claim_installers(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "packaging").mkdir()
            (root / "packaging/desktop-targets.json").write_text(
                (Path(__file__).resolve().parents[1] / "packaging/desktop-targets.json").read_text(encoding="utf-8"), encoding="utf-8")
            result = build_plan(root)
        self.assertEqual(result["status"], "blocked")
        self.assertEqual(result["installers_created"], 0)
        self.assertFalse(result["native_behavior_validated"])
        self.assertEqual(len(result["primary_packages"]), 3)

    def test_conditional_windows_dependency_cannot_be_hidden(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            project = Path(directory) / "Desktop.csproj"
            project.write_text('<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
                               '<UseWPF>true</UseWPF><UseWPF>false</UseWPF></PropertyGroup></Project>',
                               encoding="utf-8")
            self.assertTrue(portable_project_errors(project))
            project.write_text('<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
                               '</PropertyGroup></Project>', encoding="utf-8")
            self.assertEqual(portable_project_errors(project), [])
            project.write_text("<Project>", encoding="utf-8")
            self.assertTrue(portable_project_errors(project))


if __name__ == "__main__":
    unittest.main()

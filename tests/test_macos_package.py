"""Exercise Mac bundle signing order with an isolated payload and inert tools."""
from __future__ import annotations
import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("package_macos", Path(__file__).resolve().parents[1] / "tools/package_macos.py")
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class MacBundleTests(unittest.TestCase):
    def test_all_payload_files_are_signed_before_apphost(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            payload = root / "artifacts/osx-arm64"
            payload.mkdir(parents=True)
            for name in ("MonitorMate.Desktop", "System.Diagnostics.Contracts.dll", "libcoreclr.dylib", "MonitorMate.Desktop.runtimeconfig.json"):
                (payload / name).write_text("fixture", encoding="utf-8")
            (root / "LICENSE").write_text("fixture license", encoding="utf-8")
            calls: list[list[str]] = []
            def run(arguments: list[str], **kwargs: object) -> subprocess.CompletedProcess[str]:
                calls.append(arguments)
                return subprocess.CompletedProcess(arguments, 0)
            with patch.object(MODULE, "__file__", str(root / "tools/package_macos.py")), patch("sys.argv", ["package_macos.py", "arm64"]), patch.object(MODULE.subprocess, "run", side_effect=run):
                MODULE.main()
            signed = [Path(call[-1]).name for call in calls if call[0] == "codesign" and "--sign" in call]
            self.assertIn("System.Diagnostics.Contracts.dll", signed)
            self.assertIn("MonitorMate.Desktop.runtimeconfig.json", signed)
            self.assertLess(signed.index("System.Diagnostics.Contracts.dll"), signed.index("MonitorMate.Desktop"))
            self.assertEqual(signed[-1], "MonitorMate (Apple Silicon).app")


if __name__ == "__main__":
    unittest.main()

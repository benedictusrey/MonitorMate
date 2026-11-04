"""Ensure an upgrade never force-kills the existing display controller."""

from __future__ import annotations

import subprocess
import tempfile
import unittest
from pathlib import Path


class InstallerTests(unittest.TestCase):
    def test_running_copy_requires_normal_exit(self) -> None:
        script = Path(__file__).resolve().parents[1] / "dist/Install-MonitorMate.ps1"
        with tempfile.TemporaryDirectory() as directory:
            driver = Path(directory) / "driver.ps1"
            driver.write_text(
                "function global:Get-CimInstance { [pscustomobject]@{ ProcessId=424242; CommandLine=\"$env:LOCALAPPDATA\\Programs\\MonitorMate\\MonitorMate.dll\" } }\n"
                "function global:Stop-Process { throw 'FORCED_STOP_CALLED' }\n"
                "function global:New-Item { throw 'UNEXPECTED_INSTALL_WRITE' }\n"
                "try { & $args[0] } catch { Write-Output $_.Exception.Message; exit 1 }\n",
                encoding="utf-8",
            )
            result = subprocess.run(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(driver), str(script)],
                                    capture_output=True, text=True, timeout=30, check=False)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Exit MonitorMate from the tray", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()

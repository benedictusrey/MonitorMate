"""Exercise publish-failure handling without running a compiler or packaging tool."""

from __future__ import annotations

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class StorePackageTests(unittest.TestCase):
    def test_failed_publish_preserves_previous_msix(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            store = root / "store"
            store.mkdir()
            (store / "Assets").mkdir()
            (store / "AppxManifest.xml").write_text("<Package />", encoding="utf-8")
            script = Path(__file__).resolve().parents[1] / "store/Build-StorePackage.ps1"
            shutil.copy2(script, store / script.name)
            previous = store / "MonitorMate.msix"
            previous.write_bytes(b"last-known-good-package")
            driver = root / "driver.ps1"
            driver.write_text(
                "function global:dotnet { $global:LASTEXITCODE = 17 }\n"
                "try { & $args[0] } catch { Write-Output $_.Exception.Message; exit 1 }\n",
                encoding="utf-8",
            )
            result = subprocess.run(
                ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(driver), str(store / script.name)],
                capture_output=True, text=True, timeout=30, check=False,
            )
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Publish failed with exit code 17", result.stdout + result.stderr)
            self.assertTrue(previous.is_file(), "Failed publish deleted the previous package.")
            self.assertEqual(previous.read_bytes(), b"last-known-good-package")


if __name__ == "__main__":
    unittest.main()

"""Native artifact reuse must fail closed on changed inputs or invalid evidence."""

from __future__ import annotations

import importlib.util
import io
import os
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location("native_reuse", Path(__file__).resolve().parents[1] / "tools/reuse_verified_arm.py")
assert SPEC is not None and SPEC.loader is not None
reuse = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(reuse)


class NativeReuseTests(unittest.TestCase):
    def test_changed_or_missing_inputs_require_native_build(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            def git(*arguments: str) -> str:
                return subprocess.check_output(["git", *arguments], cwd=root, text=True, stderr=subprocess.DEVNULL).strip()
            git("init")
            git("config", "user.name", "Test")
            git("config", "user.email", "test@example.invalid")
            (root / "input").write_text("original", encoding="utf-8")
            git("add", ".")
            git("commit", "-m", "original")
            original = git("rev-parse", "HEAD")
            with patch.object(reuse, "SOURCE", original), patch.object(reuse, "INPUTS", ("input",)):
                self.assertTrue(reuse.same_inputs(root))
                (root / "input").write_text("changed", encoding="utf-8")
                git("add", ".")
                git("commit", "-m", "changed")
                self.assertFalse(reuse.same_inputs(root))
            with patch.object(reuse, "SOURCE", original), patch.object(reuse, "INPUTS", ("missing",)):
                self.assertFalse(reuse.same_inputs(root))
            with patch.object(reuse, "SOURCE", original), patch.object(reuse, "INPUTS", ("input",)), patch.object(reuse, "ROOT_CONFIGS", ("input",)):
                self.assertFalse(reuse.same_inputs(root))

    def test_failed_or_different_source_build_is_not_reused(self) -> None:
        for run in ({"conclusion": "failure", "head_sha": reuse.SOURCE}, {"conclusion": "success", "head_sha": "different"}):
            with patch.object(reuse, "api", return_value=run):
                self.assertIsNone(reuse.verified_artifact())

    def test_native_success_and_unexpired_digest_are_required(self) -> None:
        for conclusion, expired, digest in (("cancelled", False, "sha256:abc"), ("success", True, "sha256:abc"), ("success", False, None)):
            responses = [{"conclusion": "success", "head_sha": reuse.SOURCE}, {"jobs": [{"name": "portable (osx-arm64)", "conclusion": conclusion}]}, {"artifacts": [{"name": "osx-arm64-package", "expired": expired, "digest": digest}]}]
            with patch.object(reuse, "api", side_effect=responses):
                self.assertIsNone(reuse.verified_artifact())

    def test_hashed_successful_bundle_is_selected(self) -> None:
        artifact = {"name": "osx-arm64-package", "expired": False, "digest": "sha256:abc"}
        responses = [{"conclusion": "success", "head_sha": reuse.SOURCE}, {"jobs": [{"name": "portable (osx-arm64)", "conclusion": "success"}]}, {"artifacts": [artifact]}]
        with patch.object(reuse, "api", side_effect=responses):
            self.assertEqual(reuse.verified_artifact(), artifact)

    def test_tampered_archive_is_rejected_before_extraction(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "osx-arm64.zip"
            opener = Mock()
            opener.open.return_value = io.BytesIO(b"tampered archive")
            artifact = {"archive_download_url": "https://api.github.com/repos/benedictusrey/MonitorMate/actions/artifacts/1/zip", "digest": "sha256:" + "0" * 64}
            with patch.dict(os.environ, {"GH_TOKEN": "test-token"}), patch.object(reuse.urllib.request, "build_opener", return_value=opener):
                with self.assertRaises(ValueError):
                    reuse.download(artifact, target)
            self.assertFalse(target.exists())


if __name__ == "__main__":
    unittest.main()

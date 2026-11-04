"""Reuse one successful native ARM bundle only when its build/test inputs match."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

REPOSITORY = "benedictusrey/MonitorMate"
SOURCE = "28608c9f6b5aa1d8e60bccbe4dbdfee3437893a9"
RUN_ID = 41847427336
INPUTS = (
    "src/MonitorMate.Desktop", "src/MonitorMate.Reminders", "licenses", "LICENSE",
    "THIRD_PARTY_NOTICES.md", "VERSION", "packaging/macos.entitlements",
    "tools/package_macos.py", "packaging/AppRun", "packaging/linux.desktop",
    "tests/MonitorMate.Desktop.Tests", "tests/MonitorMate.Reminders.Tests",
)
ROOT_CONFIGS = ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "NuGet.Config", "nuget.config")


def same_inputs(root: Path) -> bool:
    """Require identical tracked inputs and no unaccounted root build configuration."""
    def object_id(revision: str, name: str) -> str | None:
        result = subprocess.run(["git", "rev-parse", "--verify", f"{revision}:{name}"], cwd=root, text=True, capture_output=True, check=False)
        return result.stdout.strip() if result.returncode == 0 else None

    if any(object_id("HEAD", name) is not None or object_id(SOURCE, name) is not None for name in ROOT_CONFIGS):
        return False
    return all(object_id("HEAD", name) is not None and object_id("HEAD", name) == object_id(SOURCE, name) for name in INPUTS)


def api(path: str) -> dict:
    """Read same-repository build evidence without exposing the workflow token."""
    request = urllib.request.Request("https://api.github.com/repos/" + REPOSITORY + path, headers={
        "Authorization": "Bearer " + os.environ["GH_TOKEN"], "User-Agent": "MonitorMate-native-reuse",
        "Accept": "application/vnd.github+json",
    })
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def verified_artifact() -> dict | None:
    """Require successful original native execution and an unexpired hashed bundle."""
    run = api(f"/actions/runs/{RUN_ID}")
    if run["conclusion"] != "success" or run["head_sha"] != SOURCE:
        return None
    jobs = api(f"/actions/runs/{RUN_ID}/jobs?per_page=100")["jobs"]
    if not any("osx-arm64" in job["name"] and job["conclusion"] == "success" for job in jobs):
        return None
    artifacts = api(f"/actions/runs/{RUN_ID}/artifacts?per_page=100")["artifacts"]
    return next((item for item in artifacts if item["name"] == "osx-arm64-package" and not item["expired"] and str(item.get("digest", "")).startswith("sha256:")), None)


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, file_pointer, code, message, headers, new_url):
        return None


def download(artifact: dict, output: Path) -> None:
    """Verify the API archive digest before copying only the expected nested ZIP."""
    request = urllib.request.Request(artifact["archive_download_url"], headers={"Authorization": "Bearer " + os.environ["GH_TOKEN"], "User-Agent": "MonitorMate-native-reuse"})
    try:
        response = urllib.request.build_opener(NoRedirect).open(request, timeout=30)
    except urllib.error.HTTPError as error:
        if error.code != 302:
            raise
        # Artifact storage receives no GitHub credential.
        response = urllib.request.urlopen(error.headers["Location"], timeout=60)
    output.parent.mkdir(parents=True, exist_ok=True)
    archive = output.parent / "verified-arm-artifact.zip"
    with response, archive.open("wb") as destination:
        shutil.copyfileobj(response, destination)
    if "sha256:" + hashlib.sha256(archive.read_bytes()).hexdigest() != artifact["digest"]:
        raise ValueError("Native ARM artifact digest does not match GitHub's evidence.")
    with zipfile.ZipFile(archive) as package:
        matches = [item for item in package.namelist() if Path(item).name == "osx-arm64.zip"]
        if len(matches) != 1:
            raise ValueError("Unexpected native ARM artifact layout.")
        with package.open(matches[0]) as source, output.open("wb") as destination:
            shutil.copyfileobj(source, destination)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--download", action="store_true")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    artifact = verified_artifact() if same_inputs(root) else None
    if args.download:
        if artifact is None:
            raise RuntimeError("Native ARM reuse evidence changed; a fresh native build is required.")
        download(artifact, root / "artifacts/osx-arm64.zip")
        print(f"Reused verified native ARM bundle: commit {SOURCE}, successful run {RUN_ID}, artifact {artifact['id']}.")
        return
    reuse = artifact is not None
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write(f"reuse={'true' if reuse else 'false'}\nrunner={'macos-26-intel' if reuse else 'macos-26'}\n")
    print("Verified unchanged native ARM build available." if reuse else "Fresh native ARM build required.")


if __name__ == "__main__":
    main()

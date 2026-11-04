"""Build an ad-hoc-signed preview app bundle from a native .NET publish."""
from __future__ import annotations
import argparse
import plistlib
import shutil
import subprocess
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("architecture", choices=("arm64", "x64"))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    source = root / "artifacts" / ("osx-" + args.architecture)
    label = "Apple Silicon" if args.architecture == "arm64" else "Intel"
    app = root / "artifacts" / ("MonitorMate (" + label + ").app")
    assert not app.exists(), "Bundle staging must be fresh."
    macos = app / "Contents" / "MacOS"
    resources = app / "Contents" / "Resources"
    shutil.copytree(source, macos)
    resources.mkdir()
    iconset = root / "artifacts" / "MonitorMate.iconset"
    iconset.mkdir(exist_ok=True)
    for size in (16, 32, 128, 256, 512):
        for scale in (1, 2):
            suffix = "@2x" if scale == 2 else ""
            target = iconset / f"icon_{size}x{size}{suffix}.png"
            subprocess.run(["sips", "-z", str(size*scale), str(size*scale), str(root / "src/MonitorMate.Desktop/Assets/monitormate.png"), "--out", str(target)], check=True, capture_output=True)
    subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o", str(resources / "MonitorMate.icns")], check=True)
    info = {"CFBundleExecutable": "MonitorMate.Desktop", "CFBundleIdentifier": "dev.benedictusrey.monitormate.preview." + args.architecture,
            "CFBundleName": "MonitorMate", "CFBundleDisplayName": "MonitorMate Reminder Preview", "CFBundlePackageType": "APPL",
            "CFBundleVersion": "1.0.0", "CFBundleShortVersionString": "1.0.0", "CFBundleIconFile": "MonitorMate.icns", "NSHighResolutionCapable": True,
            "LSMinimumSystemVersion": "14.0"}
    (app / "Contents/Info.plist").write_bytes(plistlib.dumps(info))
    shutil.copy2(root / "LICENSE", resources / "LICENSE.txt")
    executable = macos / "MonitorMate.Desktop"
    executable.chmod(0o755)
    # MacOS bundle contents are nested code, including managed assemblies.
    for item in sorted(macos.rglob("*")):
        if item.is_file() and item != executable:
            subprocess.run(["codesign", "--force", "--sign", "-", str(item)], check=True)
    subprocess.run(["codesign", "--force", "--sign", "-", "--entitlements", str(root / "packaging/macos.entitlements"), str(executable)], check=True)
    subprocess.run(["codesign", "--force", "--sign", "-", "--entitlements", str(root / "packaging/macos.entitlements"), str(app)], check=True)
    subprocess.run(["codesign", "--verify", "--deep", "--strict", str(app)], check=True)
    print(app)


if __name__ == "__main__":
    main()

"""Check local Markdown targets and the four-image MonitorMate README."""

from __future__ import annotations

import argparse
import re
import tempfile
from pathlib import Path
from urllib.parse import unquote, urlsplit

IGNORED_DIRECTORIES = {".git", ".dotnet", ".nuget", "bin", "obj", "__pycache__", "artifacts", "test-data", ".taste-skill"}
LINK_PATTERN = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
FENCE_PATTERN = re.compile(r"^```[^\n]*\n.*?^```[^\n]*$", re.MULTILINE | re.DOTALL)


def local_targets(markdown: str) -> list[str]:
    """Extract inline local targets, excluding fenced examples and external URLs.

    shortcut: reference-style links are not parsed; extend this when such links are introduced.
    """
    content = FENCE_PATTERN.sub("", markdown)
    targets = [match.group(1).split(' "', 1)[0].strip("<>")
               for match in LINK_PATTERN.finditer(content)]
    targets.extend(re.findall(r"\[!\[[^\]]*\]\([^)]+\)\]\(([^)]+)\)", content))
    return [unquote(target.split("#", 1)[0]) for target in targets
            if not urlsplit(target).scheme and not target.startswith("#")]


def check_document(path: Path, root: Path) -> list[str]:
    """Reject absent targets, traversal outside the repository and unclosed fences."""
    content = path.read_text(encoding="utf-8-sig")
    errors: list[str] = []
    if len(re.findall(r"^```", content, re.MULTILINE)) % 2:
        errors.append(f"{path.relative_to(root)}: unclosed code fence")
    for target in local_targets(content):
        if not target:
            continue
        destination = (path.parent / target).resolve()
        if not destination.is_relative_to(root):
            errors.append(f"{path.relative_to(root)}: target escapes repository: {target}")
        elif not destination.exists():
            errors.append(f"{path.relative_to(root)}: missing target: {target}")
    return errors


def self_test() -> None:
    """Exercise spaces, external links, examples, missing paths and path traversal."""
    sample = '[local](guide.md) [space](<My%20File.md>) [web](https://example.com) [part](#section)'
    assert local_targets(sample) == ["guide.md", "My File.md"]
    assert local_targets("[![badge](https://img.shields.io/badge/status-preview)](guide.md)") == ["guide.md"]
    assert local_targets('```markdown\n[example](absent.md)\n```') == []
    with tempfile.TemporaryDirectory() as directory:
        root = Path(directory).resolve()
        document = root / "README.md"
        (root / "guide.md").write_text("# Guide\n", encoding="utf-8")
        document.write_text("# Title\n\n[Guide](guide.md)\n", encoding="utf-8")
        assert check_document(document, root) == []
        document.write_text("[Missing](absent.md) [Escape](../outside.md)", encoding="utf-8")
        assert len(check_document(document, root)) == 2
        document.write_text("```\nunfinished", encoding="utf-8")
        assert "unclosed code fence" in check_document(document, root)[0]
    print("Documentation checker self-test passed.")


def main() -> int:
    """Run checks without editing files, contacting GitHub or starting MonitorMate."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true")
    arguments = parser.parse_args()
    if arguments.self_test:
        self_test()
        return 0
    root = Path(__file__).resolve().parents[1]
    documents = sorted(path for path in root.rglob("*.md")
                       if not IGNORED_DIRECTORIES.intersection(path.relative_to(root).parts))
    errors = [error for path in documents for error in check_document(path, root)]
    readme = (root / "README.md").read_text(encoding="utf-8")
    images = [target for target in re.findall(r"!\[[^\]]+\]\(([^)]+)\)", readme)
              if not target.startswith("https://img.shields.io/")]
    expected_images = {
        "docs/images/monitormate-overview.png", "docs/images/monitormate-comfort.png",
        "docs/images/monitormate-focus.png", "docs/screenshots/today.png",
    }
    if len(images) != 4 or set(images) != expected_images:
        errors.append("README must contain exactly the four documented image paths.")
    for error in errors:
        print(error)
    print(f"Checked {len(documents)} Markdown files and {len(images)} README images; {len(errors)} errors.")
    return int(bool(errors))


if __name__ == "__main__":
    raise SystemExit(main())

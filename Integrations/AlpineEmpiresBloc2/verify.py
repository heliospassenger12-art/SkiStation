"""Compare the Bloc 2 kit, its Unity parent and a Git revision without editing them."""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess


KIT = Path(__file__).resolve().parent
PARENT_MANIFEST = "ALPINE_EMPIRES_BLOC_2_SAUVEGARDE/MANIFEST_BLOC_02_SAUVEGARDE.json"


def digest(path):
    if not path.is_file():
        return None
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def git_digest(path):
    result = hashlib.sha1()
    result.update(f"blob {path.stat().st_size}\0".encode())
    with path.open("rb") as stream:
        while chunk := stream.read(1024 * 1024):
            result.update(chunk)
    return result.hexdigest()


def project_file(root, relative):
    path = (root / relative).resolve()
    if not path.is_relative_to(root):
        raise ValueError(f"Path outside project: {relative}")
    return path


def verify(project, repository, revision):
    manifest = json.loads((KIT / "MANIFEST.json").read_text(encoding="utf-8"))
    parent = digest(project / PARENT_MANIFEST)
    files = []
    for item in manifest["files"]:
        relative = item["path"]
        current = digest(project_file(project, relative))
        payload = digest(project_file(KIT / "Payload", relative))
        state = "diverged"
        if current == item["after"]:
            state = "installed"
        elif current == item["before"]:
            state = "parent"
        files.append({"path": relative, "project_state": state,
                      "project_sha256": current, "payload_sha256": payload,
                      "payload_valid": payload == item["after"]})

    version_path = project / "ProjectSettings/ProjectVersion.txt"
    version_lines = version_path.read_text(encoding="utf-8-sig").splitlines()
    valid_version = "m_EditorVersion: 6000.3.7f1" in version_lines
    files_valid = all(item["payload_valid"] for item in files)
    parent_ready = (parent == manifest["parentManifestSha256"] and valid_version
                    and files_valid and all(item["project_state"] == "parent" for item in files))
    installed = (parent == manifest["parentManifestSha256"] and valid_version
                 and files_valid and all(item["project_state"] == "installed" for item in files))

    revision_sha = subprocess.check_output(
        ["git", "rev-parse", "--verify", revision + "^{commit}"], cwd=repository,
        text=True).strip()
    entries = subprocess.check_output(
        ["git", "ls-tree", "-r", "-z", revision_sha], cwd=repository)
    comparison = []
    tracked = set()
    totals = {"identical": 0, "different": 0, "repository_only": 0}
    for entry in entries.split(b"\0"):
        if not entry:
            continue
        metadata, raw_path = entry.split(b"\t", 1)
        _, kind, blob_sha = metadata.decode().split()
        if kind != "blob":
            continue
        relative = raw_path.decode("utf-8")
        tracked.add(relative)
        source = project_file(project, relative)
        state = "repository_only"
        if source.is_file():
            state = "identical" if git_digest(source) == blob_sha else "different"
        totals[state] += 1
        comparison.append({"path": relative, "state": state})

    source_paths = {path.relative_to(project).as_posix()
                    for folder in ("Assets", "Packages", "ProjectSettings")
                    for path in (project / folder).rglob("*") if path.is_file()}
    return {
        "repository_revision": revision_sha,
        "required_editor": "6000.3.7f1",
        "editor_matches": valid_version,
        "parent_manifest_sha256": parent,
        "parent_manifest_matches": parent == manifest["parentManifestSha256"],
        "kit_payload_valid": files_valid,
        "installer_parent_ready": parent_ready,
        "installer_already_applied": installed,
        "unity_tested": False,
        "bloc_3_gate": "NO_GO",
        "installer_files": files,
        "comparison_summary": totals,
        "unity_source_files": len(source_paths),
        "unity_files_absent_from_repository": sorted(source_paths - tracked),
        "repository_files": comparison,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--repository", type=Path, default=KIT.parents[1])
    parser.add_argument("--ref", default="main", help="Git revision to compare")
    parser.add_argument("--output", type=Path, help="Optional JSON audit output outside the Unity project")
    args = parser.parse_args()
    project = args.project.resolve()
    try:
        if args.output and args.output.resolve().is_relative_to(project):
            raise ValueError("Write the audit output outside the Unity project.")
        report = verify(project, args.repository.resolve(), args.ref)
        if args.output:
            args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({key: report[key] for key in (
            "repository_revision", "parent_manifest_matches", "kit_payload_valid",
            "installer_parent_ready", "installer_already_applied", "comparison_summary",
            "unity_source_files", "bloc_3_gate")}, ensure_ascii=False, indent=2))
        return 0 if report["installer_parent_ready"] or report["installer_already_applied"] else 2
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Audit failed: {error}\n")


if __name__ == "__main__":
    raise SystemExit(main())

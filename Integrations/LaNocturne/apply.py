"""Install or check the native La Nocturne integration on the verified Unity base."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile


ROOT = Path(__file__).resolve().parent


def digest(data):
    return hashlib.sha256(data).hexdigest()


def replace_file(path, data):
    scratch = None
    try:
        with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as file:
            scratch = Path(file.name)
            file.write(data)
        scratch.chmod(path.stat().st_mode)
        os.replace(scratch, path)
    finally:
        if scratch is not None:
            scratch.unlink(missing_ok=True)


def install(project, check):
    manifest = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
    version = (project / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8-sig")
    if f"m_EditorVersion: {manifest['unity']}" not in version.splitlines():
        raise ValueError(f"Version Unity attendue : {manifest['unity']}")

    pending = []
    for relative, hashes in manifest["files"].items():
        destination = (project / relative).resolve()
        if project not in destination.parents:
            raise ValueError(f"Fichier hors projet : {relative}")
        original = destination.read_bytes()
        replacement = (ROOT / "Unity" / relative).read_bytes()
        if digest(replacement) != hashes["after"]:
            raise ValueError(f"Intégration incomplète ou modifiée : {relative}")
        current = digest(original)
        if current == hashes["after"]:
            continue
        if current != hashes["before"]:
            raise ValueError(f"Base différente ou fichier modifié : {relative}")
        pending.append((destination, original, replacement))

    if check:
        print(f"Base compatible. {len(pending)} fichier(s) à appliquer.")
        return

    installed = []
    try:
        for destination, original, replacement in pending:
            replace_file(destination, replacement)
            installed.append((destination, original))
        for destination, _, replacement in pending:
            if destination.read_bytes() != replacement:
                raise OSError(f"Vérification après écriture : {destination}")
    except OSError:
        for destination, original in reversed(installed):
            replace_file(destination, original)
        raise
    print(f"La Nocturne : {len(installed)} fichier(s) mis à jour.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path, help="Dossier Unity contenant Assets et ProjectSettings")
    parser.add_argument("--check", action="store_true", help="Vérifier les fichiers sans les modifier")
    args = parser.parse_args()
    try:
        install(args.project.resolve(), args.check)
    except (OSError, ValueError) as error:
        parser.exit(1, f"{error}\n")


if __name__ == "__main__":
    main()

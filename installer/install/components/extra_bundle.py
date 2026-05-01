from __future__ import annotations

from collections.abc import Mapping
from pathlib import Path
import shutil
import tempfile
from urllib.parse import urlparse

from .. import services as installer_services


_EXTRA_BUNDLE_ARCHIVE_EXTENSIONS = {".zip", ".7z"}


def _normalize_bundle_key(value: object) -> str:
    return str(value or "").strip().casefold()


def _resolve_bundle_entry(
    module_download_links: Mapping[str, object],
    bundle_key: str,
) -> Mapping[str, object] | None:
    entry = module_download_links.get(bundle_key)
    return entry if isinstance(entry, Mapping) else None


def _resolve_download_filename(entry: Mapping[str, object], url: str, bundle_key: str) -> str:
    filename = Path(str(entry.get("filename", "") or "").strip()).name
    if filename:
        return filename

    parsed_name = Path(urlparse(str(url or "")).path).name
    if parsed_name:
        return parsed_name

    return f"{bundle_key}.7z"


def _copy_payload_tree(payload_dir: Path, target_dir: Path) -> None:
    for source_path in sorted(payload_dir.rglob("*"), key=lambda path: str(path.relative_to(payload_dir)).casefold()):
        relative_path = source_path.relative_to(payload_dir)
        destination_path = target_dir / relative_path

        if source_path.is_dir():
            if destination_path.exists() and not destination_path.is_dir():
                installer_services.ensure_writable(destination_path)
                destination_path.unlink()
            destination_path.mkdir(parents=True, exist_ok=True)
            continue

        destination_path.parent.mkdir(parents=True, exist_ok=True)
        if destination_path.exists() and destination_path.is_dir():
            shutil.rmtree(destination_path)
        elif destination_path.exists():
            installer_services.ensure_writable(destination_path)
        shutil.copy2(source_path, destination_path)


def install_extra_bundle(
    target_path: str,
    game_data: Mapping[str, object],
    module_download_links: Mapping[str, object],
    logger=None,
) -> bool:
    bundle_key = _normalize_bundle_key(game_data.get("extra_bundle"))
    if not bundle_key:
        return False

    target_dir = Path(str(target_path or "").strip())
    if not target_dir.is_dir():
        raise ValueError(f"Invalid target folder: {target_path}")

    entry = _resolve_bundle_entry(module_download_links, bundle_key)
    if entry is None:
        raise FileNotFoundError(f"Extra bundle resource is not configured: {bundle_key}")

    url = str(entry.get("url", "") or "").strip()
    if not url:
        raise FileNotFoundError(f"Extra bundle download URL is empty: {bundle_key}")

    filename = _resolve_download_filename(entry, url, bundle_key)
    suffix = Path(filename).suffix.lower()
    if suffix not in _EXTRA_BUNDLE_ARCHIVE_EXTENSIONS:
        raise ValueError(f"Extra bundle must be a .zip or .7z archive: {filename}")

    with tempfile.TemporaryDirectory() as tmpdir:
        tmpdir_path = Path(tmpdir)
        download_path = tmpdir_path / filename
        extract_dir = tmpdir_path / "payload"

        installer_services.download_to_file(url, str(download_path), timeout=60, logger=logger)
        installer_services.extract_archive(str(download_path), str(extract_dir), logger=logger)
        _copy_payload_tree(extract_dir, target_dir)

    if logger:
        logger.info("Installed extra bundle: %s", bundle_key)
    return True


__all__ = ["install_extra_bundle"]

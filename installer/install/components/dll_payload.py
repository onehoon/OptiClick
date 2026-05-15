from __future__ import annotations

import shutil
import uuid
from pathlib import Path
from urllib.parse import urlparse

from .. import services as installer_services
from ..archive_source import resolve_cached_archive_path


ARCHIVE_EXTENSIONS = {".zip", ".7z"}


def normalize_dll_destination_path(target_path: str | Path, destination_rel_path: object) -> Path:
    target_dir = Path(target_path).resolve(strict=False)
    if not target_dir.is_dir():
        raise ValueError(f"Invalid target folder: {target_path}")

    raw = str(destination_rel_path or "").strip().replace("\\", "/")
    while raw.startswith("./"):
        raw = raw[2:]
    if not raw:
        raise ValueError("Invalid DLL destination path: (empty)")

    relative = Path(raw)
    if relative.is_absolute() or raw.startswith("/"):
        raise ValueError(f"Invalid DLL destination path: {destination_rel_path}")
    if any(part == ".." for part in relative.parts):
        raise ValueError(f"Invalid DLL destination path: {destination_rel_path}")
    if not relative.name or relative.name.lower().endswith(".dll") is False:
        raise ValueError(f"Invalid DLL destination path: {destination_rel_path}")

    destination = (target_dir / relative).resolve(strict=False)
    destination.relative_to(target_dir)
    return destination


def install_dll_payload_from_archive(
    *,
    target_path: str | Path,
    destination_rel_path: object,
    source_dll_name: str,
    url: str = "",
    cached_archive_path: str = "",
    download_filename: str = "",
    logger=None,
    temp_prefix: str = ".opticlick_dll_payload_tmp_",
) -> bool:
    target_dir = Path(target_path).resolve(strict=False)
    if not target_dir.is_dir():
        raise ValueError(f"Invalid target folder: {target_path}")
    destination_path = normalize_dll_destination_path(target_dir, destination_rel_path)

    cached = resolve_cached_archive_path(cached_archive_path)
    normalized_url = str(url or "").strip()
    if cached is None and not normalized_url:
        raise FileNotFoundError("Download link is not configured")

    tmpdir_path = target_dir / f"{temp_prefix}{uuid.uuid4().hex}"
    tmpdir_path.mkdir(parents=False, exist_ok=False)
    try:
        source_path = cached
        if source_path is None:
            file_name = Path(str(download_filename or "").strip()).name
            if not file_name:
                file_name = Path(urlparse(normalized_url).path).name
            if not file_name:
                file_name = source_dll_name
            source_path = tmpdir_path / file_name
            installer_services.download_to_file(normalized_url, str(source_path), timeout=60, logger=logger)

        candidates: list[Path] = []
        if source_path.suffix.lower() in ARCHIVE_EXTENSIONS:
            extract_dir = tmpdir_path / "payload"
            installer_services.extract_archive(str(source_path), str(extract_dir), logger=logger)
            candidates = [p for p in extract_dir.rglob("*") if p.is_file() and p.name.lower() == source_dll_name.lower()]
        elif source_path.name.lower() == source_dll_name.lower():
            candidates = [source_path]
        else:
            raise FileNotFoundError(f"{source_dll_name} was not found inside the archive")

        if not candidates:
            raise FileNotFoundError(f"{source_dll_name} was not found inside the archive")
        if len(candidates) > 1:
            names = ", ".join(sorted(str(p).replace("\\", "/") for p in candidates))
            raise RuntimeError(f"Multiple {source_dll_name} payload candidates were found: {names}")

        destination_path.parent.mkdir(parents=True, exist_ok=True)
        if destination_path.exists():
            if not destination_path.is_file():
                raise RuntimeError(f"Destination is not a file: {destination_path}")
            installer_services.ensure_writable(destination_path)
        shutil.copy2(candidates[0], destination_path)
        if logger:
            logger.info("Installed DLL payload to %s", destination_path)
        return True
    finally:
        shutil.rmtree(tmpdir_path, ignore_errors=True)

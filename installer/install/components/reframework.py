from __future__ import annotations

from pathlib import Path
from typing import Mapping

from .. import services as installer_services
from ._link_utils import extract_module_url
from .dll_payload import install_dll_payload_from_archive, normalize_dll_destination_path

REFRAMEWORK_SOURCE_DLL_NAME = "dinput8.dll"


def _is_real_reshade_dll(file_path: Path) -> bool:
    version_info = installer_services.read_windows_version_strings(file_path)
    if not version_info:
        return False
    text = " ".join(str(value or "") for value in version_info.values()).casefold()
    return "reshade" in text


def _remove_legacy_reframework_candidate(candidate_path: Path, *, logger=None) -> None:
    try:
        installer_services.ensure_writable(candidate_path)
        candidate_path.unlink()
    except OSError as exc:
        raise RuntimeError(f"Failed to remove legacy REFramework DLL: {candidate_path}") from exc
    if logger:
        logger.info("Removed legacy REFramework DLL: %s", candidate_path)


def cleanup_legacy_reframework_files(
    *,
    target_path: str | Path,
    current_destination_rel_path: str,
    logger=None,
) -> None:
    target_dir = Path(target_path).resolve(strict=False)
    if not target_dir.is_dir():
        raise ValueError(f"Invalid target folder: {target_path}")

    current_destination_path = normalize_dll_destination_path(target_dir, current_destination_rel_path)
    current_name = current_destination_path.name.casefold()

    if current_name == "reshade64.dll":
        candidate_path = target_dir / "dinput8.dll"
        if candidate_path == current_destination_path or not candidate_path.exists():
            return
        if not candidate_path.is_file():
            if logger:
                logger.info("Skipped REFramework legacy cleanup because candidate is not a file: %s", candidate_path)
            return
        _remove_legacy_reframework_candidate(candidate_path, logger=logger)
        return

    if current_name == "dinput8.dll":
        candidate_path = target_dir / "ReShade64.dll"
        if candidate_path == current_destination_path or not candidate_path.exists():
            return
        if not candidate_path.is_file():
            if logger:
                logger.info("Skipped REFramework legacy cleanup because candidate is not a file: %s", candidate_path)
            return
        if _is_real_reshade_dll(candidate_path):
            if logger:
                logger.info("Skipped REFramework legacy cleanup for real ReShade DLL: %s", candidate_path)
            return
        _remove_legacy_reframework_candidate(candidate_path, logger=logger)


def install_reframework_dinput8(
    target_path: str,
    game_data: Mapping[str, object],
    module_download_links: Mapping[str, object],
    logger=None,
    cached_archive_path: str = "",
) -> bool:
    destination_rel_path = str(game_data.get("reframework_url", "") or "").strip()
    if not destination_rel_path:
        return False
    destination_path = normalize_dll_destination_path(target_path, destination_rel_path)
    destination_rel_path = destination_path.relative_to(Path(target_path).resolve(strict=False)).as_posix()
    url = extract_module_url(module_download_links, "reframework")
    entry = module_download_links.get("reframework") if isinstance(module_download_links, Mapping) else {}
    download_filename = str((entry or {}).get("filename", "") or "").strip()
    if not cached_archive_path and not url:
        raise FileNotFoundError("REFramework download link is not configured")
    cleanup_legacy_reframework_files(
        target_path=target_path,
        current_destination_rel_path=destination_rel_path,
        logger=logger,
    )
    install_dll_payload_from_archive(
        target_path=target_path,
        destination_rel_path=destination_rel_path,
        source_dll_name=REFRAMEWORK_SOURCE_DLL_NAME,
        url=url,
        cached_archive_path=cached_archive_path,
        download_filename=download_filename,
        logger=logger,
        temp_prefix=".opticlick_reframework_tmp_",
    )
    if logger:
        logger.info("Installed REFramework to %s", destination_rel_path)
    return True

from __future__ import annotations

from pathlib import Path
from typing import Mapping

from .. import services as installer_services
from ._link_utils import extract_module_url
from .dll_payload import install_dll_payload_from_archive, normalize_dll_destination_path

SPECIALK64_DLL_NAME = "SpecialK64.dll"
_ROOT_DXGI_DLL_NAME = "dxgi.dll"
_PLUGINS_FOLDER_NAME = "plugins"
_DIRECT_PLUGINS_DXGI_PATH = "plugins/dxgi.dll"


def _normalize_specialk_value(value: object) -> str:
    normalized = str(value or "").strip().replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]
    return normalized.strip("/")


def is_specialk_plugins_mode(value: object) -> bool:
    return _normalize_specialk_value(value).casefold() == _PLUGINS_FOLDER_NAME


def _is_specialk_dll(file_path: Path) -> bool:
    version_info = installer_services.read_windows_version_strings(file_path)
    if not version_info:
        return False
    text = " ".join(str(value or "") for value in version_info.values()).casefold()
    return "special k" in text or "specialk" in text


def resolve_specialk_destination_rel_path(
    game_data: Mapping[str, object],
    optiscaler_final_dll_name: str,
) -> str:
    normalized = _normalize_specialk_value(game_data.get("specialk", ""))
    if not normalized:
        return ""

    if normalized.casefold() == _PLUGINS_FOLDER_NAME:
        dll_name = str(optiscaler_final_dll_name or "").strip()
        if not dll_name:
            raise ValueError("OptiScaler final DLL name is required for Special K plugins install mode")
        if "/" in dll_name or "\\" in dll_name:
            raise ValueError(f"Invalid OptiScaler final DLL name for Special K plugins install mode: {dll_name}")
        if not dll_name.lower().endswith(".dll"):
            raise ValueError(f"Invalid OptiScaler final DLL name for Special K plugins install mode: {dll_name}")
        return f"{_PLUGINS_FOLDER_NAME}/{dll_name}"

    return normalized


def _build_legacy_specialk_candidates(optiscaler_final_dll_name: str) -> tuple[str, ...]:
    candidates = [_ROOT_DXGI_DLL_NAME, _DIRECT_PLUGINS_DXGI_PATH]
    dll_name = str(optiscaler_final_dll_name or "").strip()
    if dll_name and dll_name.lower().endswith(".dll"):
        candidates.append(f"{_PLUGINS_FOLDER_NAME}/{dll_name}")

    unique_candidates: list[str] = []
    seen: set[str] = set()
    for candidate in candidates:
        folded = candidate.casefold()
        if folded in seen:
            continue
        seen.add(folded)
        unique_candidates.append(candidate)
    return tuple(unique_candidates)


def cleanup_legacy_specialk_files(
    *,
    target_path: str | Path,
    current_destination_rel_path: str,
    optiscaler_final_dll_name: str,
    logger=None,
) -> None:
    target_dir = Path(target_path).resolve(strict=False)
    if not target_dir.is_dir():
        raise ValueError(f"Invalid target folder: {target_path}")

    current_destination_path = normalize_dll_destination_path(target_dir, current_destination_rel_path)
    for candidate_rel_path in _build_legacy_specialk_candidates(optiscaler_final_dll_name):
        candidate_path = normalize_dll_destination_path(target_dir, candidate_rel_path)
        if candidate_path == current_destination_path:
            continue
        if not candidate_path.exists():
            continue
        if not candidate_path.is_file():
            if logger:
                logger.info("Skipped Special K legacy cleanup because candidate is not a file: %s", candidate_path)
            continue
        if not _is_specialk_dll(candidate_path):
            if logger:
                logger.info(
                    "Skipped Special K legacy cleanup because candidate is not identified as Special K: %s",
                    candidate_path,
                )
            continue

        try:
            installer_services.ensure_writable(candidate_path)
            candidate_path.unlink()
        except OSError as exc:
            raise RuntimeError(f"Failed to remove legacy Special K DLL: {candidate_path}") from exc
        if logger:
            logger.info("Removed legacy Special K DLL: %s", candidate_path)


def install_specialk(
    target_path: str,
    game_data: Mapping[str, object],
    module_download_links: Mapping[str, object],
    logger=None,
    cached_archive_path: str = "",
    optiscaler_final_dll_name: str = "",
) -> bool:
    destination_rel_path = resolve_specialk_destination_rel_path(game_data, optiscaler_final_dll_name)
    if not destination_rel_path:
        return False
    destination_path = normalize_dll_destination_path(target_path, destination_rel_path)
    destination_rel_path = destination_path.relative_to(Path(target_path).resolve(strict=False)).as_posix()
    url = extract_module_url(module_download_links, "specialk")
    entry = module_download_links.get("specialk") if isinstance(module_download_links, Mapping) else {}
    download_filename = str((entry or {}).get("filename", "") or "").strip()
    if not cached_archive_path and not url:
        raise FileNotFoundError("Special K download link is not configured")
    cleanup_legacy_specialk_files(
        target_path=target_path,
        current_destination_rel_path=destination_rel_path,
        optiscaler_final_dll_name=optiscaler_final_dll_name,
        logger=logger,
    )
    install_dll_payload_from_archive(
        target_path=target_path,
        destination_rel_path=destination_rel_path,
        source_dll_name=SPECIALK64_DLL_NAME,
        url=url,
        cached_archive_path=cached_archive_path,
        download_filename=download_filename,
        logger=logger,
        temp_prefix=".opticlick_specialk_tmp_",
    )
    if logger:
        logger.info("Installed Special K to %s", destination_rel_path)
    return True

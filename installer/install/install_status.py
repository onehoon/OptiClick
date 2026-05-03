from __future__ import annotations

from collections.abc import Mapping
from pathlib import Path
import re
from typing import Any

from .services import OPTISCALER_MANAGED_CANDIDATE_NAMES, read_windows_version_strings


INSTALL_STATUS_INSTALLABLE = "installable"
INSTALL_STATUS_UPDATE_AVAILABLE = "update_available"
INSTALL_STATUS_LATEST = "latest"
INSTALL_STATUS_PRE_RELEASE = "pre_release"
INSTALL_STATUS_NEEDS_REVIEW = "needs_review"

OPTISCALER_STATUS_CANDIDATES = OPTISCALER_MANAGED_CANDIDATE_NAMES

_OPTISCALER_ORIGINAL_FILENAME = "optiscaler.dll"
_VERSION_TOKEN_RE = re.compile(r"\d+(?:[\.,]\d+)*")
_STATUS_LABELS = {
    "ko": {
        INSTALL_STATUS_INSTALLABLE: "\ubbf8\uc124\uce58",
        INSTALL_STATUS_UPDATE_AVAILABLE: "\uc5c5\ub370\uc774\ud2b8",
        INSTALL_STATUS_LATEST: "\ucd5c\uc2e0",
        INSTALL_STATUS_PRE_RELEASE: "Pre",
        INSTALL_STATUS_NEEDS_REVIEW: "\ud655\uc778",
    },
    "en": {
        INSTALL_STATUS_INSTALLABLE: "Not Installed",
        INSTALL_STATUS_UPDATE_AVAILABLE: "Update",
        INSTALL_STATUS_LATEST: "Latest",
        INSTALL_STATUS_PRE_RELEASE: "Pre",
        INSTALL_STATUS_NEEDS_REVIEW: "Check",
    },
}


def _normalize_lang(lang: str) -> str:
    return "ko" if str(lang or "").strip().casefold().startswith("ko") else "en"


def _label_for_status(
    code: str,
    lang: str,
    *,
    installed_version: str = "",
    current_version: str = "",
    current_display_version: str = "",
) -> str:
    normalized_code = str(code or "").strip() or INSTALL_STATUS_INSTALLABLE
    labels = _STATUS_LABELS[_normalize_lang(lang)]
    label = labels.get(normalized_code, labels[INSTALL_STATUS_INSTALLABLE])
    if normalized_code == INSTALL_STATUS_UPDATE_AVAILABLE:
        display_version = str(current_display_version or current_version or "").strip()
        if display_version:
            return f"{label} ({display_version})"
    if normalized_code == INSTALL_STATUS_PRE_RELEASE:
        display_version = str(installed_version or "").strip()
        if display_version:
            return f"{label} ({display_version})"
    return label


def _build_status(
    code: str,
    *,
    lang: str,
    installed_version: str = "",
    current_version: str = "",
    current_display_version: str = "",
    detected_file: str = "",
    source: str = "",
) -> dict[str, str]:
    normalized_code = str(code or "").strip() or INSTALL_STATUS_INSTALLABLE
    return {
        "code": normalized_code,
        "label": _label_for_status(
            normalized_code,
            lang,
            installed_version=installed_version,
            current_version=current_version,
            current_display_version=current_display_version,
        ),
        "installed_version": str(installed_version or "").strip(),
        "current_version": str(current_version or "").strip(),
        "current_display_version": str(current_display_version or "").strip(),
        "detected_file": str(detected_file or "").strip(),
        "source": str(source or "").strip(),
    }


def _resolve_current_optiscaler_versions(module_download_links: Mapping[str, Any]) -> tuple[str, str]:
    entry = module_download_links.get("optiscaler") if isinstance(module_download_links, Mapping) else None
    if not isinstance(entry, Mapping):
        return "", ""
    current_version = str(entry.get("version", "") or "").strip()
    display_version = str(entry.get("display_version", "") or "").strip()
    return current_version, display_version


def _parse_version_tuple(value: object) -> tuple[int, ...]:
    text = str(value or "").strip()
    if not text:
        return ()

    match = _VERSION_TOKEN_RE.search(text)
    if match is None:
        return ()

    parts: list[int] = []
    for token in re.split(r"[\.,]", match.group(0)):
        if not token.isdigit():
            return ()
        parts.append(int(token))
    return tuple(parts)


def _compare_versions(left: object, right: object) -> int | None:
    left_parts = _parse_version_tuple(left)
    right_parts = _parse_version_tuple(right)
    if not left_parts or not right_parts:
        return None

    size = max(len(left_parts), len(right_parts))
    normalized_left = left_parts + (0,) * (size - len(left_parts))
    normalized_right = right_parts + (0,) * (size - len(right_parts))
    if normalized_left < normalized_right:
        return -1
    if normalized_left > normalized_right:
        return 1
    return 0


def _extract_comparable_binary_version(version_info: Mapping[str, str]) -> str:
    for key in ("FileVersion", "ProductVersion"):
        value = str(version_info.get(key, "") or "").strip()
        if value and _parse_version_tuple(value):
            return value
    return ""


def _is_optiscaler_binary_version_info(version_info: Mapping[str, str]) -> bool:
    original_filename = str(version_info.get("OriginalFilename", "") or "").strip().casefold()
    return original_filename == _OPTISCALER_ORIGINAL_FILENAME


def _iter_existing_status_candidates(target_dir: Path) -> tuple[Path, ...]:
    return tuple(
        target_dir / name
        for name in OPTISCALER_STATUS_CANDIDATES
        if (target_dir / name).is_file()
    )


def resolve_game_install_status(
    game_data: Mapping[str, Any],
    module_download_links: Mapping[str, Any],
    *,
    lang: str = "ko",
) -> dict[str, str]:
    current_version, current_display_version = _resolve_current_optiscaler_versions(module_download_links)
    target_dir = Path(str(game_data.get("path", "") or "").strip())
    if not target_dir.is_dir():
        return _build_status(
            INSTALL_STATUS_INSTALLABLE,
            current_version=current_version,
            current_display_version=current_display_version,
            lang=lang,
        )

    optiscaler_detections: list[tuple[Path, str, int | None]] = []
    for candidate_path in _iter_existing_status_candidates(target_dir):
        version_info = read_windows_version_strings(candidate_path)
        if not _is_optiscaler_binary_version_info(version_info):
            continue

        installed_version = _extract_comparable_binary_version(version_info)
        comparison = _compare_versions(installed_version, current_version)
        optiscaler_detections.append((candidate_path, installed_version, comparison))

    if not optiscaler_detections:
        return _build_status(
            INSTALL_STATUS_INSTALLABLE,
            current_version=current_version,
            current_display_version=current_display_version,
            lang=lang,
        )

    for candidate_path, installed_version, comparison in optiscaler_detections:
        if comparison is None or comparison < 0:
            return _build_status(
                INSTALL_STATUS_UPDATE_AVAILABLE,
                installed_version=installed_version,
                current_version=current_version,
                current_display_version=current_display_version,
                detected_file=candidate_path.name,
                source="binary",
                lang=lang,
            )

    candidate_path, installed_version, comparison = optiscaler_detections[0]
    if comparison is not None and comparison > 0:
        return _build_status(
            INSTALL_STATUS_PRE_RELEASE,
            installed_version=installed_version,
            current_version=current_version,
            current_display_version=current_display_version,
            detected_file=candidate_path.name,
            source="binary",
            lang=lang,
        )

    return _build_status(
        INSTALL_STATUS_LATEST,
        installed_version=installed_version,
        current_version=current_version,
        current_display_version=current_display_version,
        detected_file=candidate_path.name,
        source="binary",
        lang=lang,
    )


__all__ = [
    "INSTALL_STATUS_INSTALLABLE",
    "INSTALL_STATUS_LATEST",
    "INSTALL_STATUS_NEEDS_REVIEW",
    "INSTALL_STATUS_PRE_RELEASE",
    "INSTALL_STATUS_UPDATE_AVAILABLE",
    "OPTISCALER_STATUS_CANDIDATES",
    "resolve_game_install_status",
]

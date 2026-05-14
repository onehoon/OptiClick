from __future__ import annotations

from collections.abc import Mapping
import logging
from typing import Any
from urllib.parse import urljoin

from ..common.flag_parser import parse_bool_token
from ..common.network_utils import build_retry_session
from .game_db_keys import (
    GPU_BUNDLE_LOADED_KEY,
    GPU_BUNDLE_SUPPORTED_KEY,
    GPU_BUNDLE_VENDOR_KEY,
    GPU_PROFILE_ID_KEY,
)


_GPU_BUNDLE_SESSION = build_retry_session(total=4, backoff_factor=0.6)
_GPU_BUNDLE_CONNECT_TIMEOUT_SECONDS = 5.0
_INSTALL_PROFILE_BOOL_FIELDS = (
    "ultimate_asi_loader",
    "optipatcher",
    "specialk",
    "unreal5",
    "rtss_overlay",
)
_INSTALL_PROFILE_TEXT_FIELDS = (
    "optiscaler_dll_name",
    "reframework_url",
    "extra_bundle",
)


def _normalize_space_lower(value: object) -> str:
    return " ".join(str(value or "").split()).strip().casefold()


def _normalize_space(value: object) -> str:
    return " ".join(str(value or "").split()).strip()


def load_gpu_bundle_manifest(
    manifest_url: str,
    *,
    timeout_seconds: float = 10.0,
) -> dict[str, Any]:
    request_url = str(manifest_url or "").strip()
    if not request_url:
        raise ValueError("GPU bundle manifest URL is empty")
    read_timeout = max(float(timeout_seconds or 0.0), 1.0)
    response = _GPU_BUNDLE_SESSION.get(
        request_url,
        timeout=(_GPU_BUNDLE_CONNECT_TIMEOUT_SECONDS, read_timeout),
    )
    response.raise_for_status()
    payload = response.json()
    if not isinstance(payload, Mapping):
        raise ValueError("GPU bundle manifest response must be a JSON object")
    return dict(payload)


def resolve_gpu_bundle_rule(
    manifest: Mapping[str, Any],
    *,
    vendor: str,
    gpu_raw: str,
) -> Mapping[str, Any] | None:
    rules = manifest.get("rules")
    if not isinstance(rules, list):
        return None
    normalized_vendor = _normalize_gpu_vendor(vendor)
    normalized_gpu_raw = _normalize_space_lower(gpu_raw)
    if not normalized_vendor or not normalized_gpu_raw:
        return None

    matches: list[tuple[int, int, int, Mapping[str, Any]]] = []
    for index, rule in enumerate(rules):
        if not isinstance(rule, Mapping):
            continue
        if not _to_bool(rule.get("enabled"), True):
            continue
        if _normalize_gpu_vendor(rule.get("vendor")) != normalized_vendor:
            continue

        match_mode = str(rule.get("match_mode") or "").strip().casefold()
        match_value = _normalize_space(rule.get("match_value"))
        if not match_value:
            continue
        normalized_match_value = match_value.casefold()

        matched = False
        if match_mode == "exact":
            matched = normalized_gpu_raw == normalized_match_value
        elif match_mode == "contains":
            matched = normalized_match_value in normalized_gpu_raw
        if not matched:
            continue

        priority = _safe_int(rule.get("priority"), 100)
        matches.append((priority, -len(normalized_match_value), index, rule))

    if not matches:
        return None
    matches.sort(key=lambda item: (item[0], item[1], item[2]))
    return matches[0][3]


def _build_bundle_request_url(bundle_base_url: str) -> str:
    request_url = str(bundle_base_url or "").strip()
    if not request_url:
        raise ValueError("GPU bundle URL is empty")
    return request_url


def _normalize_gpu_vendor(value: object) -> str:
    text = str(value or "").strip().lower()
    if not text:
        return ""
    if "nvidia" in text:
        return "nvidia"
    if "intel" in text:
        return "intel"
    if "amd" in text or "radeon" in text:
        return "amd"
    return ""


def load_supported_game_bundle(
    bundle_base_url: str,
    gpu_vendor: str,
    gpu_model: str,
    *,
    manifest_url: str | None = None,
    request_source: str | None = None,
    device_manufacturer: str | None = None,
    device_model: str | None = None,
    app_version: str | None = None,
    timeout_seconds: float = 10.0,
    logger: logging.Logger | None = None,
) -> dict[str, dict[str, Any]]:
    use_logger = logger or logging.getLogger(__name__)
    normalized_vendor = _normalize_gpu_vendor(gpu_vendor)
    normalized_gpu_model = _normalize_space(gpu_model)
    request_url = _build_bundle_request_url(bundle_base_url)
    manifest_request_url = str(manifest_url or "").strip() or urljoin(request_url, "/v1/gpu-bundle-manifest")

    manifest = load_gpu_bundle_manifest(
        manifest_request_url,
        timeout_seconds=timeout_seconds,
    )
    manifest_version_text = str(manifest.get("manifest_version") or "").strip()
    use_logger.info("[GPU-BUNDLE] manifest loaded version=%s", manifest_version_text or "-")

    matched_rule = resolve_gpu_bundle_rule(
        manifest,
        vendor=normalized_vendor,
        gpu_raw=normalized_gpu_model,
    )
    if not matched_rule:
        use_logger.info(
            "[GPU-BUNDLE] no manifest match vendor=%s gpu=%s",
            normalized_vendor or "-",
            normalized_gpu_model or "-",
        )
        return {}

    bundle_key = str(matched_rule.get("bundle_key") or "").strip()
    if not bundle_key:
        raise ValueError("GPU bundle rule has empty bundle_key")
    use_logger.info(
        "[GPU-BUNDLE] resolved vendor=%s gpu=%s bundle=%s group=%s",
        normalized_vendor or "-",
        normalized_gpu_model or "-",
        bundle_key,
        str(matched_rule.get("gpu_group") or "").strip() or "-",
    )

    params = {
        "vendor": normalized_vendor,
        "bundle": bundle_key,
        "gpu_raw": normalized_gpu_model,
        "request_source": str(request_source or "").strip(),
        "device_manufacturer": str(device_manufacturer or "").strip(),
        "device_model": str(device_model or "").strip(),
        "app_version": str(app_version or "").strip(),
        "manifest_version": manifest_version_text,
    }
    read_timeout = max(float(timeout_seconds or 0.0), 1.0)
    response = _GPU_BUNDLE_SESSION.get(
        request_url,
        params=params,
        timeout=(_GPU_BUNDLE_CONNECT_TIMEOUT_SECONDS, read_timeout),
    )
    response.raise_for_status()
    payload = response.json()

    if not isinstance(payload, Mapping):
        raise ValueError("GPU bundle response must be a JSON object")

    if payload.get("ok") is False:
        raise ValueError(str(payload.get("error") or "GPU bundle request failed"))

    shared_profiles = payload.get("profiles") if isinstance(payload.get("profiles"), Mapping) else {}

    games_obj = payload.get("games")
    if games_obj is None and all(isinstance(v, Mapping) for v in payload.values()):
        # Backward-compatible format: {"ffxvi": {...}, ...}
        normalized = _normalize_bundle_games(payload, shared_profiles=shared_profiles, request_vendor=normalized_vendor)
        use_logger.info("[GPU-BUNDLE] bundle games count=%d", len(normalized))
        return normalized

    normalized = _normalize_bundle_games(games_obj, shared_profiles=shared_profiles, request_vendor=normalized_vendor)
    use_logger.info("[GPU-BUNDLE] bundle games count=%d", len(normalized))
    return normalized


def _normalize_bundle_games(
    games_obj: Any,
    *,
    shared_profiles: Mapping[str, Any] | None = None,
    request_vendor: str = "",
) -> dict[str, dict[str, Any]]:
    shared_profiles = shared_profiles or {}
    bundle: dict[str, dict[str, Any]] = {}
    normalized_request_vendor = _normalize_gpu_vendor(request_vendor)

    if isinstance(games_obj, Mapping):
        items = list(games_obj.values())
    elif isinstance(games_obj, list):
        items = games_obj
    else:
        items = []

    for raw in items:
        if not isinstance(raw, Mapping):
            continue

        game_id = str(raw.get("game_id") or "").strip()
        if not game_id:
            continue

        entry = dict(raw)
        if not _normalize_gpu_vendor(entry.get("bundle_gpu_vendor")) and normalized_request_vendor:
            entry["bundle_gpu_vendor"] = normalized_request_vendor
        if shared_profiles and "shared_profiles" not in entry:
            entry["shared_profiles"] = dict(shared_profiles)
        bundle[game_id.casefold()] = entry

    return bundle


def _to_bool(value: object, default: bool = False) -> bool:
    return parse_bool_token(
        value,
        empty_default=default,
        unknown_default=default,
    )


def _safe_int(value: object, default: int = 100) -> int:
    try:
        return int(str(value).strip())
    except Exception:
        return default


def _normalize_profile_key(value: object) -> str:
    return str(value or "").strip().casefold()


def _resolve_layered_optiscaler_ini_rows(bundle_entry: Mapping[str, Any]) -> list[dict[str, Any]]:
    local_rows = [row for row in list(bundle_entry.get("optiscaler_ini") or []) if isinstance(row, Mapping)]

    shared_profiles = bundle_entry.get("shared_profiles")
    if not isinstance(shared_profiles, Mapping):
        return [dict(row) for row in local_rows]

    shared_ini_rows = [row for row in list(shared_profiles.get("optiscaler_ini") or []) if isinstance(row, Mapping)]
    if not shared_ini_rows:
        return [dict(row) for row in local_rows]

    game_id = str(bundle_entry.get("game_id") or "").strip()
    profile_id = str(bundle_entry.get("profile_id") or "").strip()
    vendor = _normalize_gpu_vendor(bundle_entry.get("bundle_gpu_vendor"))

    active_profile_ids = {"global_all"}
    if vendor and vendor not in {"all", "default"}:
        active_profile_ids.add(f"global_{vendor}")
    if game_id:
        active_profile_ids.add(f"{game_id.casefold()}_all")
    if profile_id:
        active_profile_ids.add(profile_id.casefold())

    layered_rows = []
    for row in shared_ini_rows:
        profile_key = _normalize_profile_key(row.get("profile_id"))
        if profile_key and profile_key in active_profile_ids:
            layered_rows.append(dict(row))

    layered_rows.extend(dict(row) for row in local_rows)
    return layered_rows


def _materialize_ini_settings(rows: list[dict[str, Any]]) -> dict[str, str]:
    selected: dict[tuple[str, str], tuple[int, str]] = {}
    for row in rows:
        section = str(row.get("section") or "").strip()
        key = str(row.get("key") or "").strip()
        if not section or not key:
            continue

        composite_key = (section, key)
        priority = _safe_int(row.get("priority"), 100)
        value = str(row.get("value") or "")

        current = selected.get(composite_key)
        if current is None or priority < current[0]:
            selected[composite_key] = (priority, value)

    return {f"{section}:{key}": value for (section, key), (_priority, value) in selected.items()}


def _apply_install_profile(game_entry: dict[str, Any], install_profile: Mapping[str, Any]) -> None:
    for field_name in _INSTALL_PROFILE_TEXT_FIELDS:
        if field_name in install_profile:
            game_entry[field_name] = str(install_profile.get(field_name) or "").strip()

    for field_name in _INSTALL_PROFILE_BOOL_FIELDS:
        if field_name in install_profile:
            game_entry[field_name] = _to_bool(install_profile.get(field_name), False)


def merge_gpu_bundle_into_game_db(
    game_db: dict[str, dict[str, Any]],
    bundle: Mapping[str, Mapping[str, Any]] | None,
) -> dict[str, dict[str, Any]]:
    merged: dict[str, dict[str, Any]] = {key: dict(value) for key, value in dict(game_db or {}).items()}
    normalized_bundle = {
        str((value or {}).get("game_id") or "").casefold(): dict(value)
        for value in dict(bundle or {}).values()
        if isinstance(value, Mapping) and str((value or {}).get("game_id") or "").strip()
    }

    game_id_index: dict[str, list[str]] = {}
    for game_key, game_entry in merged.items():
        game_entry[GPU_BUNDLE_LOADED_KEY] = True
        game_entry[GPU_BUNDLE_SUPPORTED_KEY] = False

        game_id = str(game_entry.get("game_id") or "").strip().casefold()
        if game_id:
            game_id_index.setdefault(game_id, []).append(game_key)

    for game_id, bundle_entry in normalized_bundle.items():
        target_keys = game_id_index.get(game_id, [])
        if not target_keys:
            continue

        install_profile = bundle_entry.get("install_profile") if isinstance(bundle_entry.get("install_profile"), Mapping) else {}
        is_enabled = _to_bool(install_profile.get("enabled"), True)

        for target_key in target_keys:
            game_entry = merged[target_key]
            game_entry[GPU_BUNDLE_LOADED_KEY] = True
            game_entry[GPU_BUNDLE_SUPPORTED_KEY] = bool(is_enabled)
            game_entry[GPU_PROFILE_ID_KEY] = str(bundle_entry.get("profile_id") or "").strip()
            normalized_vendor = _normalize_gpu_vendor(bundle_entry.get("bundle_gpu_vendor"))
            if normalized_vendor:
                game_entry[GPU_BUNDLE_VENDOR_KEY] = normalized_vendor

            _apply_install_profile(game_entry, install_profile)

            layered_optiscaler_ini = _resolve_layered_optiscaler_ini_rows(bundle_entry)
            game_entry["ini_settings"] = _materialize_ini_settings(layered_optiscaler_ini)

    return merged


__all__ = [
    "load_gpu_bundle_manifest",
    "resolve_gpu_bundle_rule",
    "load_supported_game_bundle",
    "merge_gpu_bundle_into_game_db",
]

from __future__ import annotations

from dataclasses import dataclass, field
import json
from pathlib import Path

from ..common.network_utils import add_github_raw_data_cache_bust, get_shared_retry_session


_DEFAULT_FALLBACK_TITLE = "Desktop PC"
_REMOTE_SESSION = get_shared_retry_session()
_HIDE_SENTINEL = "__HIDE__"


@dataclass(frozen=True)
class DeviceIdentityRules:
    manufacturer_aliases: dict[str, str] = field(default_factory=dict)
    model_aliases: dict[str, str] = field(default_factory=dict)
    logo_keys: dict[str, str] = field(default_factory=dict)


def _normalize_lookup_key(value: object) -> str:
    return " ".join(str(value or "").split()).strip().upper()


def _normalize_text(value: object) -> str:
    return " ".join(str(value or "").split()).strip()


def _normalize_rule_mapping(mapping: object) -> dict[str, str]:
    if not isinstance(mapping, dict):
        return {}

    normalized: dict[str, str] = {}
    for key, value in mapping.items():
        normalized_key = _normalize_lookup_key(key)
        normalized_value = _normalize_text(value)
        if not normalized_key or not normalized_value:
            continue
        normalized[normalized_key] = normalized_value
    return normalized


def _build_rules_from_payload(payload: object) -> DeviceIdentityRules:
    if not isinstance(payload, dict):
        return DeviceIdentityRules()

    return DeviceIdentityRules(
        manufacturer_aliases=_normalize_rule_mapping(payload.get("manufacturer_aliases")),
        model_aliases=_normalize_rule_mapping(payload.get("model_aliases")),
        logo_keys=_normalize_rule_mapping(payload.get("logo_keys")),
    )


def load_device_identity_rules_from_file(path: str | Path) -> DeviceIdentityRules:
    rules_path = Path(path)
    if not rules_path.exists():
        return DeviceIdentityRules()
    payload = json.loads(rules_path.read_text(encoding="utf-8-sig"))
    return _build_rules_from_payload(payload)


def load_device_identity_rules_from_remote(source_url: str, *, timeout_seconds: float = 3.0) -> DeviceIdentityRules:
    normalized_url = str(source_url or "").strip()
    if not normalized_url:
        return DeviceIdentityRules()
    response = _REMOTE_SESSION.get(add_github_raw_data_cache_bust(normalized_url), timeout=timeout_seconds)
    response.raise_for_status()
    return _build_rules_from_payload(json.loads(response.content.decode("utf-8-sig")))


def merge_device_identity_rules(base: DeviceIdentityRules, override: DeviceIdentityRules) -> DeviceIdentityRules:
    merged_manufacturers = dict(base.manufacturer_aliases)
    merged_manufacturers.update(override.manufacturer_aliases)

    merged_models = dict(base.model_aliases)
    merged_models.update(override.model_aliases)

    merged_logo_keys = dict(base.logo_keys)
    merged_logo_keys.update(override.logo_keys)

    return DeviceIdentityRules(
        manufacturer_aliases=merged_manufacturers,
        model_aliases=merged_models,
        logo_keys=merged_logo_keys,
    )


def normalize_device_manufacturer(raw_manufacturer: str, rules: DeviceIdentityRules) -> str:
    normalized_raw = _normalize_text(raw_manufacturer)
    if not normalized_raw:
        return ""
    return rules.manufacturer_aliases.get(_normalize_lookup_key(normalized_raw), normalized_raw)


def normalize_device_model(raw_model: str, rules: DeviceIdentityRules) -> str:
    normalized_raw = _normalize_text(raw_model)
    if not normalized_raw:
        return ""
    resolved = rules.model_aliases.get(_normalize_lookup_key(normalized_raw), normalized_raw)
    if _normalize_lookup_key(resolved) == _HIDE_SENTINEL:
        return ""
    return resolved


def build_device_title(
    raw_manufacturer: str,
    raw_model: str,
    rules: DeviceIdentityRules,
    *,
    fallback_title: str = _DEFAULT_FALLBACK_TITLE,
) -> str:
    display_manufacturer = normalize_device_manufacturer(raw_manufacturer, rules)
    display_model = normalize_device_model(raw_model, rules)

    if display_manufacturer and display_model:
        if display_model.casefold().startswith(display_manufacturer.casefold()):
            return display_model
        return f"{display_manufacturer} {display_model}"
    if display_model:
        return display_model
    if display_manufacturer:
        return display_manufacturer
    return str(fallback_title or _DEFAULT_FALLBACK_TITLE).strip() or _DEFAULT_FALLBACK_TITLE


def resolve_device_logo_key(raw_manufacturer: str, rules: DeviceIdentityRules) -> str:
    display_manufacturer = normalize_device_manufacturer(raw_manufacturer, rules)
    if not display_manufacturer:
        return ""
    return rules.logo_keys.get(_normalize_lookup_key(display_manufacturer), "")


__all__ = [
    "DeviceIdentityRules",
    "build_device_title",
    "load_device_identity_rules_from_file",
    "load_device_identity_rules_from_remote",
    "merge_device_identity_rules",
    "normalize_device_manufacturer",
    "normalize_device_model",
    "resolve_device_logo_key",
]

from __future__ import annotations

from typing import Any

import requests

from installer.common.network_utils import build_retry_session


OPTICLICK_RUNTIME_DATA_URL = "https://opticlick-data-api.onehoon.workers.dev/v1/runtime-data"
CLOUDFLARE_STATUS_SUMMARY_URL = "https://www.cloudflarestatus.com/api/v2/summary.json"
REQUIRED_RUNTIME_KEYS = {
    "engine_ini_profile",
    "game_ini_profile",
    "game_json_profile",
    "game_master",
    "game_unreal_ini_profile",
    "game_xml_profile",
    "message_binding",
    "message_center",
    "registry_profile",
    "resource_master",
}


class RuntimeDataError(RuntimeError):
    pass


class RuntimeDataNetworkError(RuntimeDataError):
    pass


class RuntimeDataHttpError(RuntimeDataError):
    pass


class RuntimeDataParseError(RuntimeDataError):
    pass


class RuntimeDataSchemaError(RuntimeDataError):
    pass


class RuntimeDataLoadFailed(RuntimeDataError):
    pass


def validate_runtime_data_payload(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        raise RuntimeDataSchemaError("runtime-data payload must be an object")

    if payload.get("schema_version") != 1:
        raise RuntimeDataSchemaError("unsupported runtime-data schema_version")

    data = payload.get("data")
    if not isinstance(data, dict):
        raise RuntimeDataSchemaError("runtime-data.data must be an object")

    missing = sorted(REQUIRED_RUNTIME_KEYS - set(data.keys()))
    if missing:
        raise RuntimeDataSchemaError(f"runtime-data missing keys: {missing}")

    for key in REQUIRED_RUNTIME_KEYS:
        value = data.get(key)
        if not isinstance(value, list):
            raise RuntimeDataSchemaError(f"runtime-data.{key} must be a list")

    return dict(data)


def load_runtime_data(url: str = OPTICLICK_RUNTIME_DATA_URL, *, timeout_seconds: float = 5.0) -> dict[str, Any]:
    session = build_retry_session(
        total=3,
        backoff_factor=0.5,
        status_forcelist=(429, 500, 502, 503, 504),
        allowed_methods=("GET",),
    )
    try:
        response = session.get(
            str(url or "").strip(),
            timeout=timeout_seconds,
            headers={"Accept": "application/json", "User-Agent": "OptiClick"},
        )
    except requests.RequestException as exc:
        raise RuntimeDataNetworkError(f"failed to download runtime-data: {exc}") from exc
    except Exception as exc:
        raise RuntimeDataLoadFailed(f"unexpected runtime-data request failure: {exc}") from exc

    try:
        response.raise_for_status()
    except requests.HTTPError as exc:
        raise RuntimeDataHttpError(f"runtime-data HTTP error: {response.status_code}") from exc
    except Exception as exc:
        raise RuntimeDataLoadFailed(f"unexpected runtime-data HTTP failure: {exc}") from exc

    try:
        payload = response.json()
    except ValueError as exc:
        raise RuntimeDataParseError(f"failed to parse runtime-data JSON: {exc}") from exc
    except Exception as exc:
        raise RuntimeDataLoadFailed(f"unexpected runtime-data parse failure: {exc}") from exc

    return validate_runtime_data_payload(payload)


def check_cloudflare_status(*, timeout_seconds: float = 3.0) -> dict[str, str]:
    try:
        session = build_retry_session(total=1, backoff_factor=0)
        response = session.get(
            CLOUDFLARE_STATUS_SUMMARY_URL,
            timeout=timeout_seconds,
            headers={"Accept": "application/json", "User-Agent": "OptiClick"},
        )
        response.raise_for_status()
        payload = response.json()
    except Exception:
        return {"indicator": "unknown", "description": ""}

    status = payload.get("status") if isinstance(payload, dict) else {}
    if not isinstance(status, dict):
        status = {}
    return {
        "indicator": str(status.get("indicator") or "unknown"),
        "description": str(status.get("description") or ""),
    }


__all__ = [
    "OPTICLICK_RUNTIME_DATA_URL",
    "RuntimeDataError",
    "RuntimeDataHttpError",
    "RuntimeDataLoadFailed",
    "RuntimeDataNetworkError",
    "RuntimeDataParseError",
    "RuntimeDataSchemaError",
    "check_cloudflare_status",
    "load_runtime_data",
    "validate_runtime_data_payload",
]

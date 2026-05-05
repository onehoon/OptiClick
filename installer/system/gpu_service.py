from __future__ import annotations

from dataclasses import dataclass
import fnmatch
import json
import logging
import os
import re
import subprocess

from ..common.process_utils import subprocess_no_window_kwargs


_ALLOWED_VENDOR_KEYWORDS = (
    "intel", "amd", "nvidia",
    "arc",
    "radeon",
    "geforce", "rtx",
)

_VENDOR_KEYWORD_MAP = {
    "nvidia": ("nvidia", "geforce", "rtx"),
    "amd": ("amd", "radeon"),
    "intel": ("intel", "arc"),
}

_VENDOR_PRIORITY = ("nvidia", "amd", "intel")
_TEST_GPU_ENABLED_ENV = "DUAL_GPU_TEST"
_TEST_GPU_NAMES_ENV = "TEST_GPU_NAMES"
_TEST_DEVICE_INFO_ENABLED_ENV = "DEVICE_INFO_TEST"
_TEST_DEVICE_MANUFACTURER_ENV = "TEST_DEVICE_MANUFACTURER"
_TEST_DEVICE_MODEL_ENV = "TEST_DEVICE_MODEL"


@dataclass(frozen=True)
class GpuAdapterChoice:
    vendor: str
    model_name: str
    display_name: str


@dataclass(frozen=True)
class GpuContext:
    gpu_names: list[str]
    gpu_count: int
    gpu_info: str
    selected_vendor: str
    adapters: tuple[GpuAdapterChoice, ...] = ()
    selected_model_name: str = ""
    device_info: HardwareDeviceInfo | None = None

    @property
    def is_multi_gpu(self) -> bool:
        return self.gpu_count > 1


@dataclass(frozen=True)
class HardwareDeviceInfo:
    manufacturer: str
    model: str


def _is_truthy_env(name: str) -> bool:
    raw = str(os.environ.get(name, "") or "").strip().lower()
    return raw in {"1", "true", "yes", "on"}


def _get_test_gpu_names_override() -> list[str]:
    if not _is_truthy_env(_TEST_GPU_ENABLED_ENV):
        return []

    raw = str(os.environ.get(_TEST_GPU_NAMES_ENV, "") or "").strip()
    if not raw:
        return []

    normalized = raw.replace("\r", "\n")
    tokens = re.split(r"[|\n]+", normalized)

    gpu_names: list[str] = []
    seen_names = set()
    for token in tokens:
        name = _normalize_text(token)
        if not name:
            continue
        dedupe_key = name.casefold()
        if dedupe_key in seen_names:
            continue
        seen_names.add(dedupe_key)
        gpu_names.append(name)
    return gpu_names


def _get_test_device_info_override() -> HardwareDeviceInfo | None:
    if not _is_truthy_env(_TEST_DEVICE_INFO_ENABLED_ENV):
        return None

    manufacturer = _normalize_text(str(os.environ.get(_TEST_DEVICE_MANUFACTURER_ENV, "") or ""))
    model = _normalize_text(str(os.environ.get(_TEST_DEVICE_MODEL_ENV, "") or ""))
    if not manufacturer and not model:
        return None

    logging.info(
        "[Hardware] Using test device override because %s is enabled: manufacturer=%s, model=%s",
        _TEST_DEVICE_INFO_ENABLED_ENV,
        manufacturer or "Unknown",
        model or "Unknown",
    )
    return HardwareDeviceInfo(
        manufacturer=manufacturer,
        model=model,
    )


def _run_powershell_cim_query(command_text: str, *, timeout: int = 8) -> list[dict[str, object]]:
    if os.name != "nt":
        return []

    try:
        result = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-Command",
                command_text,
            ],
            capture_output=True,
            text=True,
            timeout=timeout,
            **subprocess_no_window_kwargs(),
        )
        if result.returncode != 0:
            return []

        payload = str(result.stdout or "").strip()
        if not payload:
            return []

        parsed = json.loads(payload)
        if isinstance(parsed, dict):
            return [parsed]
        if isinstance(parsed, list):
            return [row for row in parsed if isinstance(row, dict)]
    except Exception:
        pass

    return []


def _get_windows_video_controller_rows() -> list[dict[str, object]]:
    return _run_powershell_cim_query(
        "Get-CimInstance Win32_VideoController | "
        "Select-Object Name | ConvertTo-Json -Compress"
    )


def get_device_info() -> HardwareDeviceInfo:
    test_override = _get_test_device_info_override()
    if test_override is not None:
        return test_override

    if os.name != "nt":
        return HardwareDeviceInfo(manufacturer="", model="")

    rows = _run_powershell_cim_query(
        "Get-CimInstance Win32_ComputerSystem | "
        "Select-Object Manufacturer, Model | ConvertTo-Json -Compress"
    )
    row = rows[0] if rows else {}
    return HardwareDeviceInfo(
        manufacturer=_normalize_text(str(row.get("Manufacturer", "") or "")),
        model=_normalize_text(str(row.get("Model", "") or "")),
    )


def build_gpu_display_list(gpu_rows: list[dict[str, object]]) -> str:
    display_entries: list[str] = []
    for row in gpu_rows:
        name = str(row.get("Name", "") or "").strip()
        if not name:
            continue
        lowered = name.lower()
        if not any(keyword in lowered for keyword in _ALLOWED_VENDOR_KEYWORDS):
            continue
        display_entries.append(name)
    return " | ".join(display_entries)


def log_hardware_snapshot(*, device_info: HardwareDeviceInfo, gpu_display_list: str) -> None:
    logging.info("[Hardware] Device.Manufacturer=%s", device_info.manufacturer or "Unknown")
    logging.info("[Hardware] Device.Model=%s", device_info.model or "Unknown")
    logging.info("[Hardware] GPU.DisplayList=%s", gpu_display_list or "Unknown")


def _get_graphics_adapter_snapshot_details() -> tuple[list[str], int, str, str]:
    """Return unique GPU names, detected adapter count, UI summary text, and log display text."""
    test_gpu_names = _get_test_gpu_names_override()
    if test_gpu_names:
        logging.info(
            "[GPU] Using test GPU override because %s is enabled: %s",
            _TEST_GPU_ENABLED_ENV,
            " | ".join(test_gpu_names),
        )
        return test_gpu_names, len(test_gpu_names), ", ".join(test_gpu_names), " | ".join(test_gpu_names)

    if os.name != "nt":
        return [], 0, "Unknown (non-Windows OS)", "Unknown (non-Windows OS)"

    try:
        gpu_rows = _get_windows_video_controller_rows()
        gpu_display_list = build_gpu_display_list(gpu_rows)

        gpu_names_unique = []
        seen_filtered_names = set()
        for row in gpu_rows:
            name = _normalize_text(str(row.get("Name", "") or ""))
            if not name:
                continue
            lowered = name.lower()
            if "mirage driver" in lowered:
                continue
            if any(keyword in lowered for keyword in _ALLOWED_VENDOR_KEYWORDS):
                normalized_name = " ".join(lowered.split())
                if normalized_name not in seen_filtered_names:
                    seen_filtered_names.add(normalized_name)
                    gpu_names_unique.append(name)

        if gpu_names_unique:
            # Only treat distinct GPU names as multi-GPU so duplicate WMI rows do not block installation.
            return gpu_names_unique, len(gpu_names_unique), ", ".join(gpu_names_unique), gpu_display_list or ", ".join(gpu_names_unique)
    except Exception:
        pass

    return [], 0, "Unknown", "Unknown"


def get_graphics_adapter_snapshot() -> tuple[list[str], int, str]:
    gpu_names, gpu_count, gpu_info, _gpu_display_list = _get_graphics_adapter_snapshot_details()
    return gpu_names, gpu_count, gpu_info


def _normalize_text(text: str) -> str:
    return " ".join(str(text or "").split()).strip()


def detect_gpu_vendor(gpu_name: str) -> str:
    lowered = _normalize_text(gpu_name).lower()
    if not lowered:
        return ""

    for vendor in _VENDOR_PRIORITY:
        if any(keyword in lowered for keyword in _VENDOR_KEYWORD_MAP[vendor]):
            return vendor
    return ""


def _shorten_gpu_model_name(vendor: str, model_name: str) -> str:
    text = _normalize_text(model_name)
    if not text:
        return ""

    text = re.sub(r"\((?:tm|r)\)", "", text, flags=re.IGNORECASE)
    text = text.replace("\u2122", "").replace("\u00AE", "")
    text = re.sub(r"\bcorporation\b", "", text, flags=re.IGNORECASE)
    text = _normalize_text(text)

    if vendor == "nvidia":
        text = re.sub(r"^nvidia\s+", "", text, flags=re.IGNORECASE)
        text = re.sub(r"^geforce\s+", "", text, flags=re.IGNORECASE)
    elif vendor == "amd":
        text = re.sub(r"^amd\s+", "", text, flags=re.IGNORECASE)
    elif vendor == "intel":
        text = re.sub(r"^intel\s+", "", text, flags=re.IGNORECASE)
        text = re.sub(r"\barc\s+graphics\b", "Arc", text, flags=re.IGNORECASE)

    text = re.sub(r"\bgraphics\b$", "", text, flags=re.IGNORECASE)
    text = _normalize_text(text)
    return text or _normalize_text(model_name)


def build_gpu_adapter_choices(
    gpu_names: list[str],
) -> tuple[GpuAdapterChoice, ...]:
    adapters: list[GpuAdapterChoice] = []
    for gpu_name in gpu_names:
        normalized_name = _normalize_text(gpu_name)
        if not normalized_name:
            continue

        vendor = detect_gpu_vendor(normalized_name)
        adapters.append(
            GpuAdapterChoice(
                vendor=vendor or "default",
                model_name=normalized_name,
                display_name=_shorten_gpu_model_name(vendor, normalized_name),
            )
        )

    return tuple(adapters)


def _select_preferred_adapter(adapters: tuple[GpuAdapterChoice, ...]) -> GpuAdapterChoice | None:
    for vendor in _VENDOR_PRIORITY:
        for adapter in adapters:
            if adapter.vendor == vendor:
                return adapter
    return adapters[0] if adapters else None


def detect_gpu_context() -> GpuContext:
    gpu_names, gpu_count, gpu_info, gpu_display_list = _get_graphics_adapter_snapshot_details()
    device_info = get_device_info()
    log_hardware_snapshot(
        device_info=device_info,
        gpu_display_list=gpu_display_list,
    )
    adapters = build_gpu_adapter_choices(gpu_names)

    selected_adapter = _select_preferred_adapter(adapters)
    if selected_adapter:
        selected_vendor = selected_adapter.vendor
        selected_model_name = selected_adapter.model_name
    else:
        selected_vendor = "default"
        selected_model_name = ""

    return GpuContext(
        gpu_names=list(gpu_names),
        gpu_count=max(0, int(gpu_count or 0)),
        gpu_info=gpu_info,
        selected_vendor=selected_vendor,
        adapters=adapters,
        selected_model_name=selected_model_name,
        device_info=device_info,
    )


def _split_gpu_rule_patterns(rule_text: str) -> list[str]:
    text = str(rule_text or "").strip()
    if not text:
        return []

    normalized = text.replace("\r", "\n").replace("\n", "|").replace(";", "|").replace(",", "|")
    return [token.strip().lower() for token in normalized.split("|") if token.strip()]


def matches_gpu_rule(rule_text: str, gpu_text: str) -> bool:
    patterns = _split_gpu_rule_patterns(rule_text)
    if not patterns:
        return False

    if any(pattern in {"all", "true", "yes", "1"} for pattern in patterns):
        return True

    normalized_gpu = str(gpu_text or "").strip().lower()
    if normalized_gpu in {"", "checking gpu...", "unknown"}:
        return False

    for pattern in patterns:
        if pattern in {"null", "none"}:
            continue
        if any(char in pattern for char in "*?[]"):
            if fnmatch.fnmatch(normalized_gpu, pattern):
                return True
        elif pattern in normalized_gpu:
            return True
    return False

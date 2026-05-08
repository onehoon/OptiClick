from __future__ import annotations

import importlib
import logging
import os
from datetime import date, timedelta
from pathlib import Path
import sys
import tempfile
from typing import Any, Callable


APP_VERSION = "0.4.9"
MAX_SUPPORTED_GPU_COUNT = 2
APP_CACHE_DIR_NAME = "OptiClick"
LEGACY_APP_CACHE_DIR_NAME = "OptiScalerInstaller"


def _load_generated_build_config() -> dict[str, object]:
    try:
        module = importlib.import_module("installer._generated_build_config")
        config = getattr(module, "BUILD_CONFIG", {})
        if isinstance(config, dict):
            return dict(config)
    except Exception:
        pass
    return {}


_GENERATED_BUILD_CONFIG = _load_generated_build_config()


def load_dev_env_file(load_dotenv: Callable[..., Any], *, entry_file: str | Path) -> None:
    if getattr(sys, "frozen", False):
        return
    env_path = Path(entry_file).resolve().parent / ".env"
    if env_path.exists():
        load_dotenv(env_path, override=True)


def get_runtime_config_value(name: str, default: str = "") -> str:
    if getattr(sys, "frozen", False) and name in _GENERATED_BUILD_CONFIG:
        value = _GENERATED_BUILD_CONFIG.get(name)
        if value is None:
            return default
        return str(value)

    value = os.environ.get(name)
    if value is None:
        return default
    return str(value)


def get_int_env(name: str, default: int = 0) -> int:
    raw = get_runtime_config_value(name, "")
    if not str(raw).strip():
        return default
    try:
        return int(str(raw).strip())
    except (TypeError, ValueError):
        logging.warning("[APP] Invalid integer env %s=%r, using %s", name, raw, default)
        return default


def get_bool_env(name: str, default: bool = False) -> bool:
    raw = get_runtime_config_value(name, "")
    text = str(raw).strip().lower()
    if not text:
        return default
    if text in {"1", "true", "yes", "y", "on"}:
        return True
    if text in {"0", "false", "no", "n", "off"}:
        return False
    logging.warning("[APP] Invalid boolean env %s=%r, using %s", name, raw, default)
    return default


class PrefixedLoggerAdapter(logging.LoggerAdapter):
    def process(self, msg, kwargs):
        prefix = self.extra.get("prefix", "APP")
        return f"[{prefix}] {msg}", kwargs


def get_prefixed_logger(prefix: str = "APP") -> PrefixedLoggerAdapter:
    return PrefixedLoggerAdapter(logging.getLogger(), {"prefix": prefix})


def _resolve_app_cache_dir(base_dir: Path) -> Path:
    legacy_dir = base_dir / LEGACY_APP_CACHE_DIR_NAME
    app_dir = base_dir / APP_CACHE_DIR_NAME
    if legacy_dir.exists() and not app_dir.exists():
        try:
            legacy_dir.rename(app_dir)
        except Exception:
            logging.debug("[APP] Legacy cache dir migration failed: %s -> %s", legacy_dir, app_dir)
    return app_dir


def _build_daily_log_filename(day: date) -> str:
    return f"OptiClick_{day.strftime('%Y%m%d')}.log"


def _prune_old_log_files(directory: Path, *, today: date) -> None:
    keep_names = {
        _build_daily_log_filename(today),
        _build_daily_log_filename(today - timedelta(days=1)),
    }
    for path in directory.glob("*.log"):
        if path.name in keep_names:
            continue
        if not (path.name.startswith("OptiClick_") or path.name.startswith("installer_")):
            continue
        try:
            path.unlink()
        except Exception:
            logging.debug("[APP] Failed to delete old log file: %s", path)


def init_file_logger(*, app_version: str, source_root: Path) -> Path | None:
    candidates: list[Path] = []

    try:
        if getattr(sys, "frozen", False) and hasattr(sys, "executable"):
            candidates.append(Path(sys.executable).resolve().parent)
        else:
            candidates.append(Path(source_root).resolve() / "logs")
    except Exception:
        pass

    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        candidates.append(_resolve_app_cache_dir(Path(local_app_data)))

    candidates.append(_resolve_app_cache_dir(Path(tempfile.gettempdir())))

    root_logger = logging.getLogger()
    formatter = logging.Formatter("%(asctime)s %(levelname)s: %(message)s", datefmt="%H:%M:%S")
    today = date.today()

    for directory in candidates:
        try:
            directory.mkdir(parents=True, exist_ok=True)
            _prune_old_log_files(directory, today=today)
            log_path = directory / _build_daily_log_filename(today)

            for handler in list(root_logger.handlers):
                if isinstance(handler, logging.FileHandler):
                    root_logger.removeHandler(handler)
                    try:
                        handler.close()
                    except Exception:
                        pass

            file_handler = logging.FileHandler(log_path, mode="a", encoding="utf-8")
            file_handler.setLevel(logging.INFO)
            file_handler.setFormatter(formatter)
            root_logger.addHandler(file_handler)
            get_prefixed_logger("APP").info("OptiClick version %s", app_version)
            return log_path
        except Exception as exc:
            try:
                print(f"Warning: failed to initialize file logging at {directory}: {exc}", file=sys.stderr)
            except Exception:
                pass

    return None


def configure_logging(*, app_version: str, source_root: Path) -> None:
    root = logging.getLogger()
    root.setLevel(logging.INFO)

    if not any(isinstance(handler, logging.StreamHandler) for handler in root.handlers):
        stream_handler = logging.StreamHandler(sys.stderr)
        stream_handler.setLevel(logging.INFO)
        stream_handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s: %(message)s", datefmt="%H:%M:%S"))
        root.addHandler(stream_handler)

    logging.getLogger("urllib3").setLevel(logging.WARNING)
    logging.getLogger("urllib3.connectionpool").setLevel(logging.ERROR)

    try:
        init_file_logger(app_version=app_version, source_root=source_root)
    except Exception:
        logging.exception("[APP] Failed during file logger initialization")


__all__ = [
    "APP_VERSION",
    "MAX_SUPPORTED_GPU_COUNT",
    "PrefixedLoggerAdapter",
    "configure_logging",
    "get_bool_env",
    "get_int_env",
    "get_prefixed_logger",
    "get_runtime_config_value",
    "init_file_logger",
    "load_dev_env_file",
]

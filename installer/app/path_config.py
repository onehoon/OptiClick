from __future__ import annotations

from dataclasses import dataclass
import os
from pathlib import Path
import sys
import tempfile
from typing import Callable


RuntimeConfigGetter = Callable[[str, str], str]
APP_CACHE_DIR_NAME = "OptiClick"
LEGACY_APP_CACHE_DIR_NAME = "OptiScalerInstaller"


def migrate_legacy_app_cache_dir(local_appdata_dir: Path) -> None:
    legacy_cache_dir = local_appdata_dir / LEGACY_APP_CACHE_DIR_NAME
    app_cache_dir = local_appdata_dir / APP_CACHE_DIR_NAME
    if not legacy_cache_dir.exists() or app_cache_dir.exists():
        return
    try:
        legacy_cache_dir.rename(app_cache_dir)
    except Exception:
        # Migration must never block app startup.
        pass


@dataclass(frozen=True)
class AppPathConfig:
    local_appdata_dir: Path
    app_cache_dir: Path
    optiscaler_cache_dir: Path
    fsr4_cache_dir: Path
    optipatcher_cache_dir: Path
    specialk_cache_dir: Path
    ual_cache_dir: Path
    unreal5_cache_dir: Path
    cover_cache_dir: Path
    covers_repo_raw_base_url: str
    fsr4_skip_gpu_rule: str
    app_base_dir: Path
    assets_dir: Path
    default_poster_candidates: tuple[Path, ...]
    device_identity_rules_path: Path
    device_identity_rules_url: str


def build_app_path_config(
    *,
    entry_file: str | Path,
    get_runtime_config_value: RuntimeConfigGetter,
) -> AppPathConfig:
    source_path = Path(entry_file).resolve()
    local_appdata_dir = Path(os.environ.get("LOCALAPPDATA") or tempfile.gettempdir())
    migrate_legacy_app_cache_dir(local_appdata_dir)
    app_cache_dir = local_appdata_dir / APP_CACHE_DIR_NAME
    app_base_dir = Path(getattr(sys, "_MEIPASS", source_path.parent))
    assets_dir = app_base_dir / "assets"

    covers_repo_raw_base_url = str(
        get_runtime_config_value(
            "OPTISCALER_COVERS_RAW_BASE_URL",
            "https://raw.githubusercontent.com/onehoon/OptiClick/covers/assets",
        )
        or ""
    ).strip().rstrip("/")
    device_identity_rules_url = str(
        get_runtime_config_value(
            "OPTISCALER_DEVICE_IDENTITY_RULES_URL",
            "https://raw.githubusercontent.com/onehoon/OptiClick/main/assets/data/device_identity_rules.json",
        )
        or ""
    ).strip()

    return AppPathConfig(
        local_appdata_dir=local_appdata_dir,
        app_cache_dir=app_cache_dir,
        optiscaler_cache_dir=app_cache_dir / "cache" / "optiscaler",
        fsr4_cache_dir=app_cache_dir / "cache" / "fsr4",
        optipatcher_cache_dir=app_cache_dir / "cache" / "optipatcher",
        specialk_cache_dir=app_cache_dir / "cache" / "specialk",
        ual_cache_dir=app_cache_dir / "cache" / "ultimateasiloader",
        unreal5_cache_dir=app_cache_dir / "cache" / "unreal5",
        cover_cache_dir=app_cache_dir / "cache" / "covers",
        covers_repo_raw_base_url=covers_repo_raw_base_url,
        fsr4_skip_gpu_rule="*rx 90*",
        app_base_dir=app_base_dir,
        assets_dir=assets_dir,
        default_poster_candidates=(
            assets_dir / "cover" / "default_poster.webp",
            assets_dir / "cover" / "default_poster.jpg",
            assets_dir / "cover" / "default_poster.png",
            assets_dir / "default_poster.webp",
            assets_dir / "default_poster.jpg",
            assets_dir / "default_poster.png",
        ),
        device_identity_rules_path=assets_dir / "data" / "device_identity_rules.json",
        device_identity_rules_url=device_identity_rules_url,
    )


__all__ = [
    "APP_CACHE_DIR_NAME",
    "AppPathConfig",
    "LEGACY_APP_CACHE_DIR_NAME",
    "build_app_path_config",
    "migrate_legacy_app_cache_dir",
]

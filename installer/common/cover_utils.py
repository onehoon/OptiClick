from __future__ import annotations

import hashlib
import re
from pathlib import Path


ALLOWED_COVER_IMAGE_EXTENSIONS = frozenset({".webp", ".png", ".jpg"})


def normalize_cover_filename(value: str) -> str:
    raw_name = str(value or "").strip()
    if not raw_name:
        return ""
    if raw_name.lower() in {"null", "none", "na", "n/a", "-"}:
        return ""
    if any(sep in raw_name for sep in ("/", "\\", ":")):
        return ""
    if Path(raw_name).name != raw_name:
        return ""

    suffix = Path(raw_name).suffix.lower()
    if suffix not in ALLOWED_COVER_IMAGE_EXTENSIONS:
        return ""
    return raw_name


def normalize_cover_steam_app_id(value: object) -> str:
    raw = str(value or "").strip()
    if not raw:
        return ""
    if raw.lower() in {"null", "none", "na", "n/a", "-"}:
        return ""
    if not raw.isdigit():
        return ""
    try:
        if int(raw) <= 0:
            return ""
    except Exception:
        return ""
    return raw


def normalize_cover_cache_stem(value: object) -> str:
    raw = str(value or "").strip().lower()
    if not raw:
        return ""
    normalized = re.sub(r"[^a-z0-9]+", "_", raw)
    normalized = re.sub(r"_+", "_", normalized).strip("_. ")
    if not normalized:
        return ""
    return normalized[:80].strip("_. ")


def build_cover_cache_filename(
    *,
    cover_steam_app_id: object = "",
    game_name_en: object = "",
    cover_url: object = "",
) -> str:
    normalized_app_id = normalize_cover_steam_app_id(cover_steam_app_id)
    if normalized_app_id:
        return f"{normalized_app_id}.webp"

    normalized_stem = normalize_cover_cache_stem(game_name_en)
    if normalized_stem:
        return f"{normalized_stem}.webp"

    normalized_url = str(cover_url or "").strip()
    if normalized_url:
        digest = hashlib.sha256(normalized_url.encode("utf-8")).hexdigest()[:16]
        return f"url_{digest}.webp"
    return ""

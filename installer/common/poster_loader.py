from __future__ import annotations

import hashlib
import io
import json
import logging
import threading
from dataclasses import dataclass, field
from pathlib import Path
from typing import Mapping
from urllib.parse import quote, urlparse

import requests
from PIL import Image, ImageOps
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

from .. import app_update
from .cover_utils import build_cover_cache_filename, normalize_cover_filename, normalize_cover_steam_app_id


@dataclass(slots=True)
class PosterLoaderConfig:
    cache_dir: Path
    assets_dir: Path
    default_poster_candidates: tuple[Path, ...]
    target_width: int
    target_height: int
    repo_raw_base_url: str = ""
    bundled_cover_filename_map: Mapping[str, str] = field(default_factory=dict)
    timeout_seconds: int = 10
    max_retries: int = 3
    cache_version: int = 2
    enable_memory_cache: bool = True
    memory_cache_max: int = 100


@dataclass(frozen=True, slots=True)
class PosterLoadResult:
    image: Image.Image
    is_default: bool
    should_retry: bool


def _make_default_poster_base(width: int, height: int) -> Image.Image:
    """Build a minimal fallback poster when no bundled asset exists."""
    return Image.new("RGB", (width, height), "#12161d")


def _load_default_poster_base(default_poster_path: Path, width: int, height: int) -> Image.Image:
    """Load the default poster from disk, creating it if it's missing or corrupt."""
    if default_poster_path.exists():
        try:
            return Image.open(default_poster_path).convert("RGB").resize((width, height), Image.Resampling.LANCZOS)
        except Exception:
            logging.warning("Failed to read/decode default poster at %s, will recreate it.", default_poster_path)

    try:
        default_poster_path.parent.mkdir(parents=True, exist_ok=True)
        img = _make_default_poster_base(width, height)
        suffix = default_poster_path.suffix.lower()
        if suffix == ".webp":
            img.save(default_poster_path, format="WEBP", quality=92)
        elif suffix in {".jpg", ".jpeg"}:
            img.save(default_poster_path, format="JPEG", quality=92)
        else:
            img.save(default_poster_path, format="PNG")
        return img
    except Exception as exc:
        logging.warning("Failed to create default poster asset at %s: %s", default_poster_path, exc)
        return _make_default_poster_base(width, height)


def _prepare_cover_image(img: Image.Image, target_w: int, target_h: int) -> Image.Image:
    """Convert and fit/crop cover art for UI rendering."""
    if img.mode not in {"RGB", "RGBA"}:
        img = img.convert("RGBA")
    else:
        img = img.copy()

    prefit_limit = (max(1, target_w * 2), max(1, target_h * 2))
    if img.width > prefit_limit[0] or img.height > prefit_limit[1]:
        img.thumbnail(prefit_limit, Image.Resampling.LANCZOS)

    img = ImageOps.fit(
        img,
        (target_w, target_h),
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.5),
    )
    return img.convert("RGBA")


def extract_steam_cover_asset_url(payload: object) -> str:
    candidate_paths = (
        ("assets", "library_capsule_2x"),
        ("assets", "library_capsule"),
    )
    try:
        items = (((payload or {}).get("response") or {}).get("store_items") or [])
        if not isinstance(items, list):
            return ""
        for item in items:
            if not isinstance(item, dict):
                continue
            assets = item.get("assets") or {}
            if not isinstance(assets, dict):
                continue
            asset_url_format = str(assets.get("asset_url_format", "") or "").strip()
            appid = str(item.get("appid", "") or "").strip()
            for path in candidate_paths:
                node: object = item
                for key in path:
                    if not isinstance(node, dict):
                        node = ""
                        break
                    node = node.get(key, "")
                value = str(node or "").strip()
                if value and (value.startswith("http://") or value.startswith("https://")):
                    return value
                if not value:
                    continue
                if asset_url_format and "${FILENAME}" in asset_url_format:
                    rendered_path = asset_url_format.replace("${FILENAME}", value).lstrip("/")
                    return f"https://shared.steamstatic.com/store_item_assets/{rendered_path}"
                if appid:
                    return f"https://shared.steamstatic.com/store_item_assets/steam/apps/{appid}/{value.lstrip('/')}"
    except Exception:
        return ""
    return ""


class PosterImageLoader:
    def __init__(self, config: PosterLoaderConfig):
        self._config = config
        self._cache_dir = Path(config.cache_dir)
        self._cache_dir.mkdir(parents=True, exist_ok=True)
        self._assets_dir = Path(config.assets_dir)
        self._default_poster_candidates = tuple(Path(path) for path in config.default_poster_candidates)
        if not self._default_poster_candidates:
            raise ValueError("Poster loader requires at least one default poster candidate path.")

        self._default_poster_path = next(
            (path for path in self._default_poster_candidates if path.exists()),
            self._default_poster_candidates[0],
        )
        self._bundled_cover_filename_map = {
            str(key).casefold(): str(value)
            for key, value in dict(config.bundled_cover_filename_map).items()
        }
        self._repo_raw_base_url = str(config.repo_raw_base_url or "").strip().rstrip("/")
        self._image_cache: dict[str, Image.Image] = {}
        self._image_cache_lock = threading.Lock()
        self._default_poster_base = _load_default_poster_base(
            self._default_poster_path,
            self._config.target_width,
            self._config.target_height,
        )
        self._image_session = self._build_retry_session()

    def close(self) -> None:
        try:
            self._image_session.close()
        except Exception:
            pass

    def make_placeholder_image(self) -> Image.Image:
        return self._default_poster_base.copy().convert("RGBA")

    def load(
        self,
        title: str,
        cover_filename: str,
        url: str,
        cover_steam_app_id: str = "",
        game_name_en: str = "",
    ) -> PosterLoadResult:
        """Load a poster while preserving source priority and retry semantics.

        Source order:
        1) cover_steam_app_id cache / Steam URL / Steam assets API
        2) cover_url cache / cover_url
        3) legacy cover_filename path (bundled/disk/repo)
        4) default poster
        """
        normalized_cover_steam_app_id = normalize_cover_steam_app_id(cover_steam_app_id)
        normalized_cover_filename = normalize_cover_filename(cover_filename)
        cover_url_cache_filename = build_cover_cache_filename(
            cover_steam_app_id="",
            game_name_en=game_name_en,
            cover_url=url,
        )
        # TODO: non-Steam cache filename is game_name_en-based, so cover_url changes alone
        # won't invalidate existing disk cache. Revisit with explicit cover cache versioning.
        steam_result, steam_failed_retryable = self._load_from_steam_cover(
            title=title,
            cover_steam_app_id=normalized_cover_steam_app_id,
        )
        if steam_result is not None:
            return steam_result

        cover_url_result, cover_url_failed_retryable = self._load_from_cover_url_cache_and_source(
            title=title,
            url=url,
            cache_filename=cover_url_cache_filename,
        )
        if cover_url_result is not None:
            return cover_url_result

        cover_cache_key = (
            self._poster_cache_key("cover_file", normalized_cover_filename, title=title)
            if normalized_cover_filename
            else ""
        )
        cover_result, repo_failed = self._load_from_cover_filename(
            normalized_cover_filename=normalized_cover_filename,
            cover_cache_key=cover_cache_key,
        )
        if cover_result is not None:
            return cover_result

        return self._make_default_result(
            should_retry=repo_failed or steam_failed_retryable or cover_url_failed_retryable
        )

    def _load_from_steam_cover(self, *, title: str, cover_steam_app_id: str) -> tuple[PosterLoadResult | None, bool]:
        if not cover_steam_app_id:
            return None, False
        cache_filename = f"{cover_steam_app_id}.webp"
        cache_key = self._poster_cache_key("steam_app", cover_steam_app_id, title=title)
        cached = self._load_from_disk_cover_cache(normalized_cover_filename=cache_filename, cover_cache_key=cache_key)
        if cached is not None:
            return cached, False

        direct_url = f"https://shared.steamstatic.com/store_item_assets/steam/apps/{cover_steam_app_id}/library_600x900_2x.jpg"
        result, direct_failed_retryable = self._try_download_to_cache(direct_url, cache_filename, cache_key)
        if result is not None:
            return result, False
        logging.debug("Steam cover direct URL failed: cover_steam_app_id=%s", cover_steam_app_id)

        assets_url = self._fetch_steam_assets_cover_url(cover_steam_app_id)
        if assets_url:
            result, assets_failed_retryable = self._try_download_to_cache(assets_url, cache_filename, cache_key)
            if result is not None:
                return result, False
            return None, assets_failed_retryable or direct_failed_retryable
        logging.debug("Steam cover assets URL failed: cover_steam_app_id=%s", cover_steam_app_id)
        return None, direct_failed_retryable

    def _fetch_steam_assets_cover_url(self, cover_steam_app_id: str) -> str:
        api_url = "https://api.steampowered.com/IStoreBrowseService/GetItems/v1/"
        try:
            app_id_int = int(cover_steam_app_id)
        except Exception:
            return ""
        input_json = {
            "ids": [{"appid": app_id_int}],
            "context": {"country_code": "US"},
            "data_request": {"include_assets": True},
        }
        try:
            with self._image_session.get(
                api_url,
                params={"input_json": json.dumps(input_json, separators=(",", ":"))},
                timeout=self._config.timeout_seconds,
            ) as response:
                response.raise_for_status()
                payload = response.json()
            return extract_steam_cover_asset_url(payload)
        except Exception:
            return ""

    def _try_download_to_cache(
        self, source_url: str, cache_filename: str, cache_key: str
    ) -> tuple[PosterLoadResult | None, bool]:
        try:
            image_bytes = self._download_image_bytes(source_url)
            prepared = self._load_prepared_image_from_bytes(image_bytes, cache_key)
            if prepared is None:
                return None, False
            cache_bytes = self._encode_cover_cache_webp_bytes(image_bytes)
            if cache_bytes:
                self._store_cover_cache_bytes(cache_filename, cache_bytes)
            return PosterLoadResult(prepared, False, False), False
        except Exception as exc:
            return None, self._is_retryable_download_exception(exc)

    def _load_from_cover_url_cache_and_source(
        self, *, title: str, url: str, cache_filename: str
    ) -> tuple[PosterLoadResult | None, bool]:
        if not url:
            return None, False
        cache_key = self._poster_cache_key("cover_url", url, title=title)
        if cache_filename:
            cached = self._load_from_disk_cover_cache(normalized_cover_filename=cache_filename, cover_cache_key=cache_key)
            if cached is not None:
                return cached, False

        return self._try_download_to_cache(url, cache_filename, cache_key) if cache_filename else (None, False)

    def _is_retryable_download_exception(self, exc: Exception) -> bool:
        if isinstance(exc, requests.exceptions.Timeout):
            return True
        if isinstance(exc, requests.exceptions.ConnectionError):
            return True
        if isinstance(exc, requests.exceptions.HTTPError):
            response = getattr(exc, "response", None)
            status = int(getattr(response, "status_code", 0) or 0)
            return status >= 500
        return False

    def _load_from_cover_filename(
        self,
        *,
        normalized_cover_filename: str,
        cover_cache_key: str,
    ) -> tuple[PosterLoadResult | None, bool]:
        """Resolve cover filename path sources and report repo fallback failure state."""
        repo_failed = False
        if not normalized_cover_filename:
            return None, repo_failed

        bundled_result = self._load_from_bundled_cover(
            normalized_cover_filename=normalized_cover_filename,
            cover_cache_key=cover_cache_key,
        )
        if bundled_result is not None:
            return bundled_result, repo_failed

        disk_result = self._load_from_disk_cover_cache(
            normalized_cover_filename=normalized_cover_filename,
            cover_cache_key=cover_cache_key,
        )
        if disk_result is not None:
            return disk_result, repo_failed

        repo_result, repo_failed = self._load_from_repo_cover(
            normalized_cover_filename=normalized_cover_filename,
            cover_cache_key=cover_cache_key,
        )
        return repo_result, repo_failed

    def _load_from_bundled_cover(
        self,
        *,
        normalized_cover_filename: str,
        cover_cache_key: str,
    ) -> PosterLoadResult | None:
        """Try bundled asset mapped by normalized cover filename."""
        bundled_cover_path = self._find_bundled_cover_asset(normalized_cover_filename)
        if bundled_cover_path is None:
            return None

        pil_img = self._load_prepared_image_from_path(bundled_cover_path, cover_cache_key)
        if pil_img is None:
            return None
        return PosterLoadResult(pil_img, False, False)

    def _load_from_disk_cover_cache(
        self,
        *,
        normalized_cover_filename: str,
        cover_cache_key: str,
    ) -> PosterLoadResult | None:
        """Try filename-based disk cache and delete corrupt cache file on decode failure."""
        disk_cache_path = self._get_cover_cache_path(normalized_cover_filename)
        if disk_cache_path is None or not disk_cache_path.exists():
            return None

        pil_img = self._load_prepared_image_from_path(disk_cache_path, cover_cache_key)
        if pil_img is not None:
            return PosterLoadResult(pil_img, False, False)

        try:
            disk_cache_path.unlink()
        except OSError:
            pass
        return None

    def _load_from_repo_cover(
        self,
        *,
        normalized_cover_filename: str,
        cover_cache_key: str,
    ) -> tuple[PosterLoadResult | None, bool]:
        """Try repo raw cover URL and return (result, repo_failed_for_retry_signal)."""
        repo_url = self._build_cover_repo_raw_url(normalized_cover_filename)
        if not repo_url:
            return None, False

        try:
            image_bytes = self._download_image_bytes(repo_url)
            pil_img = self._load_prepared_image_from_bytes(image_bytes, cover_cache_key)
            if pil_img is None:
                raise RuntimeError("Downloaded cover image could not be decoded")
            try:
                self._store_cover_cache_bytes(normalized_cover_filename, image_bytes)
            except Exception:
                pass
            return PosterLoadResult(pil_img, False, False), False
        except Exception:
            return None, True

    def _encode_cover_cache_webp_bytes(self, image_bytes: bytes) -> bytes | None:
        try:
            with Image.open(io.BytesIO(image_bytes)) as source_img:
                converted = source_img.convert("RGB")
            output = io.BytesIO()
            converted.save(output, format="WEBP", quality=90, method=6)
            return output.getvalue()
        except Exception:
            return None

    def _make_default_result(self, *, should_retry: bool) -> PosterLoadResult:
        """Build default poster result with explicit retry flag."""
        return PosterLoadResult(self.make_placeholder_image(), True, bool(should_retry))

    def _build_retry_session(self) -> requests.Session:
        session = requests.Session()
        retry = Retry(
            total=self._config.max_retries,
            connect=self._config.max_retries,
            read=self._config.max_retries,
            backoff_factor=0.6,
            status_forcelist=(429, 500, 502, 503, 504),
            allowed_methods=("GET", "HEAD"),
        )
        adapter = HTTPAdapter(max_retries=retry)
        session.mount("https://", adapter)
        session.mount("http://", adapter)
        return session

    def _find_bundled_cover_asset(self, cover_filename: str) -> Path | None:
        normalized = normalize_cover_filename(cover_filename)
        if not normalized:
            return None

        bundled_name = self._bundled_cover_filename_map.get(normalized.casefold())
        if not bundled_name:
            return None

        candidate = self._assets_dir / bundled_name
        if candidate.exists() and candidate.is_file():
            return candidate
        return None

    def _get_cover_cache_path(self, cover_filename: str) -> Path | None:
        normalized = normalize_cover_filename(cover_filename)
        if not normalized:
            return None
        return app_update.resolve_safe_child_path(self._cache_dir, normalized)

    def _build_cover_repo_raw_url(self, cover_filename: str) -> str:
        normalized = normalize_cover_filename(cover_filename)
        if not normalized or not self._repo_raw_base_url:
            return ""
        return f"{self._repo_raw_base_url}/{quote(normalized, safe='')}"

    def _poster_cache_key(self, source_type: str, source_value: str, title: str = "") -> str:
        normalized_source = ""
        raw_value = str(source_value or "").strip()
        if source_type == "cover_url" and raw_value:
            try:
                parsed = urlparse(raw_value)
                normalized_source = f"{parsed.scheme.lower()}://{parsed.netloc.lower()}{parsed.path}".strip().lower()
            except Exception:
                normalized_source = raw_value.lower()
        else:
            normalized_source = raw_value.casefold()

        if not normalized_source:
            normalized_source = str(title or "").strip().casefold() or "unknown"

        cache_source = (
            f"poster|v{self._config.cache_version}|"
            f"{self._config.target_width}x{self._config.target_height}|"
            f"{source_type}|{normalized_source}"
        )
        return hashlib.sha256(cache_source.encode("utf-8")).hexdigest()

    def _load_prepared_image_from_path(self, image_path: Path, cache_key: str) -> Image.Image | None:
        cached_image = self._image_cache_get(cache_key) if self._config.enable_memory_cache else None
        if cached_image is not None:
            return cached_image
        if not image_path.exists() or not image_path.is_file():
            return None

        try:
            with Image.open(image_path) as source_img:
                pil_img = _prepare_cover_image(source_img, self._config.target_width, self._config.target_height)
            if self._config.enable_memory_cache:
                self._image_cache_put(cache_key, pil_img)
            return pil_img
        except Exception:
            return None

    def _load_prepared_image_from_bytes(self, image_bytes: bytes, cache_key: str) -> Image.Image | None:
        cached_image = self._image_cache_get(cache_key) if self._config.enable_memory_cache else None
        if cached_image is not None:
            return cached_image

        try:
            with Image.open(io.BytesIO(image_bytes)) as source_img:
                pil_img = _prepare_cover_image(source_img, self._config.target_width, self._config.target_height)
            if self._config.enable_memory_cache:
                self._image_cache_put(cache_key, pil_img)
            return pil_img
        except Exception:
            return None

    def _download_image_bytes(self, url: str) -> bytes:
        with self._image_session.get(url, timeout=self._config.timeout_seconds, stream=True) as response:
            response.raise_for_status()
            return b"".join(response.iter_content(chunk_size=65536))

    def _store_cover_cache_bytes(self, cover_filename: str, image_bytes: bytes) -> Path | None:
        cache_path = self._get_cover_cache_path(cover_filename)
        if cache_path is None:
            return None

        cache_path.parent.mkdir(parents=True, exist_ok=True)
        temp_path = cache_path.with_name(cache_path.name + ".tmp")
        with temp_path.open("wb") as cache_fp:
            cache_fp.write(image_bytes)
        temp_path.replace(cache_path)
        return cache_path

    def _image_cache_get(self, key: str) -> Image.Image | None:
        try:
            with self._image_cache_lock:
                pil_img = self._image_cache.get(key)
                if pil_img is None:
                    return None
                self._image_cache.pop(key, None)
                self._image_cache[key] = pil_img
                return pil_img
        except Exception:
            return None

    def _image_cache_put(self, key: str, pil_img: Image.Image) -> None:
        try:
            with self._image_cache_lock:
                self._image_cache.pop(key, None)
                self._image_cache[key] = pil_img
                if len(self._image_cache) > self._config.memory_cache_max:
                    try:
                        first_key = next(iter(self._image_cache))
                        del self._image_cache[first_key]
                    except Exception:
                        pass
        except Exception:
            pass


__all__ = [
    "PosterImageLoader",
    "PosterLoaderConfig",
    "PosterLoadResult",
]

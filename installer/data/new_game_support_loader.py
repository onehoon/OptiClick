from __future__ import annotations

import json
from dataclasses import dataclass
from datetime import date
from typing import Any

from installer.common.network_utils import add_github_raw_data_cache_bust, get_shared_retry_session


_file_session = get_shared_retry_session()


@dataclass(frozen=True)
class NewGameSupportEntry:
    game_name_kr: str
    game_name_en: str
    detected_on: str


def _normalize_text(value: object) -> str:
    return str(value or "").strip()


def _parse_iso_date(value: object) -> date | None:
    try:
        return date.fromisoformat(_normalize_text(value))
    except ValueError:
        return None


def _fetch_remote_text(url: str, *, timeout_seconds: float = 10.0) -> str:
    normalized = _normalize_text(url)
    if not normalized:
        raise ValueError("New game support JSON URL is empty")

    response = _file_session.get(
        add_github_raw_data_cache_bust(normalized),
        timeout=timeout_seconds,
    )
    response.raise_for_status()
    return response.content.decode("utf-8-sig")


def _parse_rows_from_text(text: str) -> list[dict[str, Any]]:
    normalized = str(text or "").lstrip("\ufeff").strip()
    if not normalized:
        return []

    payload = json.loads(normalized)
    if not isinstance(payload, list):
        raise ValueError("New game support payload JSON must be a list")
    return [row for row in payload if isinstance(row, dict)]


def _parse_entry(row: dict[str, Any]) -> NewGameSupportEntry | None:
    game_name_kr = _normalize_text(row.get("game_name_kr"))
    game_name_en = _normalize_text(row.get("game_name_en"))
    detected_on = _normalize_text(row.get("detected_on"))

    if not game_name_kr and not game_name_en:
        return None
    if _parse_iso_date(detected_on) is None:
        return None

    return NewGameSupportEntry(
        game_name_kr=game_name_kr,
        game_name_en=game_name_en,
        detected_on=detected_on,
    )


def load_new_game_support(
    source_url: str = "",
    *,
    timeout_seconds: float = 5.0,
) -> tuple[NewGameSupportEntry, ...]:
    text = _fetch_remote_text(source_url, timeout_seconds=timeout_seconds)
    entries: list[NewGameSupportEntry] = []
    for row in _parse_rows_from_text(text):
        entry = _parse_entry(row)
        if entry is not None:
            entries.append(entry)
    return tuple(entries)


def _pick_entry_title(entry: NewGameSupportEntry, *, lang: str) -> str:
    normalized_lang = str(lang or "").lower()
    if normalized_lang.startswith("ko"):
        return entry.game_name_kr or entry.game_name_en
    return entry.game_name_en or entry.game_name_kr


def build_new_game_support_popup_text(
    entries: tuple[NewGameSupportEntry, ...] | list[NewGameSupportEntry],
    *,
    lang: str,
) -> str:
    normalized_entries = tuple(entries or ())
    if not normalized_entries:
        return ""

    is_ko = str(lang or "").lower().startswith("ko")
    title = "신규 지원 게임" if is_ko else "New Game Support"
    lines = [f"[RED]{title}[END]"]
    seen_titles: set[str] = set()

    for entry in normalized_entries:
        game_title = _pick_entry_title(entry, lang=lang).strip()
        if not game_title:
            continue
        dedupe_key = game_title.casefold()
        if dedupe_key in seen_titles:
            continue
        seen_titles.add(dedupe_key)
        lines.append(f"[INDENT][DOT]{game_title}")

    if len(lines) <= 1:
        return ""
    return "\n".join(lines)


__all__ = [
    "NewGameSupportEntry",
    "build_new_game_support_popup_text",
    "load_new_game_support",
]

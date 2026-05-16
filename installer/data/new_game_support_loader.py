from __future__ import annotations

from dataclasses import dataclass
from datetime import date
from typing import Any


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


def parse_new_game_support_rows(rows: object) -> tuple[NewGameSupportEntry, ...]:
    if not isinstance(rows, list):
        return tuple()

    entries: list[NewGameSupportEntry] = []
    for row in rows:
        if not isinstance(row, dict):
            continue
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
    "parse_new_game_support_rows",
]

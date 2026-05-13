from __future__ import annotations

from collections.abc import Callable, Mapping
from dataclasses import dataclass
from typing import Any

import customtkinter as ctk


CardIndexCallback = Callable[[int], None]
PosterQueueCallback = Callable[[int, Any, str, str, str, str, str], None]
PlaceholderImageFactory = Callable[[], Any]

_STATUS_BADGE_STYLES = {
    "installable": ("#2E7D5B", "#F4FFF8"),
    "update_available": ("#D6AA43", "#0B121A"),
    "latest": ("#243447", "#FFFFFF"),
    "pre_release": ("#4A5D7A", "#FFFFFF"),
    "needs_review": ("#9B4D56", "#FFFFFF"),
}
_DEFAULT_STATUS_BADGE_STYLE = ("#2A303A", "#E3EAF3")
_STATUS_BADGE_HEIGHT = 20
_STATUS_BADGE_HORIZONTAL_PAD = 4
_STATUS_BADGE_X = 0
_STATUS_BADGE_Y = 0


@dataclass(frozen=True)
class GameCardTheme:
    card_width: int
    card_height: int
    card_background: str
    title_overlay_background: str
    title_overlay_text_color: str
    title_font_family: str
    title_font_size: int = 11
    title_wrap_width: int = 0
    title_height: int = 34


@dataclass(frozen=True)
class GameCardBuildResult:
    card: Any
    card_item: dict[str, Any]


def _resolve_status_badge_style(install_status: Mapping[str, Any]) -> tuple[str, str]:
    code = str((install_status or {}).get("code", "") or "").strip()
    return _STATUS_BADGE_STYLES.get(code, _DEFAULT_STATUS_BADGE_STYLE)


def _is_wide_status_badge_char(char: str) -> bool:
    codepoint = ord(char)
    return (
        0x1100 <= codepoint <= 0x11FF
        or 0x3130 <= codepoint <= 0x318F
        or 0xAC00 <= codepoint <= 0xD7AF
        or 0x4E00 <= codepoint <= 0x9FFF
    )


def _estimate_status_badge_width(text: str) -> int:
    text_width = 0
    for char in str(text or ""):
        if char.isspace():
            text_width += 3
        elif _is_wide_status_badge_char(char):
            text_width += 9
        elif char in "().,/":
            text_width += 4
        else:
            text_width += 5
    return text_width + (_STATUS_BADGE_HORIZONTAL_PAD * 2)


def create_game_card(
    *,
    parent: Any,
    index: int,
    game: Mapping[str, Any],
    theme: GameCardTheme,
    make_placeholder_image: PlaceholderImageFactory,
    on_select: CardIndexCallback,
    on_activate: CardIndexCallback,
    on_hover_enter: CardIndexCallback,
    on_hover_leave: CardIndexCallback,
    queue_poster: PosterQueueCallback,
) -> GameCardBuildResult:
    display_name = str(game["display"])
    install_status = dict(game.get("install_status") or {})
    status_label = str(install_status.get("label", "") or "").strip()
    status_fg_color, status_text_color = _resolve_status_badge_style(install_status)
    card = ctk.CTkFrame(
        parent,
        width=int(theme.card_width),
        fg_color=theme.card_background,
        corner_radius=0,
        border_width=2,
        border_color=theme.card_background,
    )
    card.grid_propagate(False)
    card.configure(height=int(theme.card_height))

    img_label = ctk.CTkLabel(card, text="", width=int(theme.card_width), height=int(theme.card_height))
    img_label.grid(row=0, column=0, padx=0, pady=0)

    hover_title = ctk.CTkLabel(
        card,
        text=display_name,
        font=ctk.CTkFont(family=theme.title_font_family, size=int(theme.title_font_size), weight="bold"),
        text_color=theme.title_overlay_text_color,
        fg_color=theme.title_overlay_background,
        corner_radius=0,
        wraplength=int(theme.title_wrap_width),
        justify="center",
        width=int(theme.card_width),
        height=int(theme.title_height),
    )
    hover_title.place_forget()

    status_badge = ctk.CTkLabel(
        card,
        text=status_label,
        font=ctk.CTkFont(family=theme.title_font_family, size=9, weight="bold"),
        text_color=status_text_color,
        fg_color=status_fg_color,
        bg_color=status_fg_color,
        corner_radius=0,
        width=_estimate_status_badge_width(status_label),
        height=_STATUS_BADGE_HEIGHT,
    )
    if status_label:
        status_badge.place(x=_STATUS_BADGE_X, y=_STATUS_BADGE_Y)
    else:
        status_badge.place_forget()

    def _handle_select(_event=None, idx=index) -> None:
        on_select(idx)

    def _handle_activate(_event=None, idx=index) -> None:
        on_activate(idx)

    def _handle_hover_enter(_event=None, idx=index) -> None:
        on_hover_enter(idx)

    def _handle_hover_leave(_event=None, idx=index) -> None:
        on_hover_leave(idx)

    for widget in (card, img_label, hover_title, status_badge):
        widget.bind("<Button-1>", _handle_select)
        widget.bind("<Double-Button-1>", _handle_activate)
        widget.bind("<Enter>", _handle_hover_enter)
        widget.bind("<Leave>", _handle_hover_leave)

    queue_poster(
        index,
        img_label,
        display_name,
        str(game.get("filename_cover", "") or ""),
        str(game.get("cover_url", "") or ""),
        str(game.get("cover_steam_app_id", "") or ""),
        str(game.get("game_name_en", "") or ""),
    )

    return GameCardBuildResult(
        card=card,
        card_item={
            "card": card,
            "img_label": img_label,
            "hover_title": hover_title,
            "status_badge": status_badge,
            "install_status": install_status,
            "base_pil": make_placeholder_image(),
            "base_revision": 0,
            "ctk_img": None,
            "ctk_img_cache_revision": -1,
            "current_image_state": None,
        },
    )


__all__ = [
    "GameCardBuildResult",
    "GameCardTheme",
    "create_game_card",
]

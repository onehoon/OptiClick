from __future__ import annotations

from collections.abc import MutableMapping
from dataclasses import dataclass
from typing import Any

import customtkinter as ctk

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
class GameCardVisualTheme:
    card_background: str
    card_width: int
    card_height: int
    title_overlay_y: int


def _resolve_status_badge_style(install_status: MutableMapping[str, Any]) -> tuple[str, str]:
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


def render_game_card_status_badge(item: MutableMapping[str, Any]) -> None:
    status_badge = item.get("status_badge")
    if status_badge is None:
        return

    install_status = item.get("install_status") or {}
    status_label = str(install_status.get("label", "") or "").strip()
    if not status_label:
        status_badge.place_forget()
        return

    fg_color, text_color = _resolve_status_badge_style(install_status)
    status_badge.configure(
        text=status_label,
        fg_color=fg_color,
        bg_color=fg_color,
        text_color=text_color,
        width=_estimate_status_badge_width(status_label),
        height=_STATUS_BADGE_HEIGHT,
    )
    status_badge.place(x=_STATUS_BADGE_X, y=_STATUS_BADGE_Y)
    status_badge.lift()


def ensure_game_card_image_cache(
    item: MutableMapping[str, Any],
    *,
    theme: GameCardVisualTheme,
    image_refs: list[Any],
) -> None:
    base_revision = int(item.get("base_revision", 0))
    if item.get("ctk_img_cache_revision") == base_revision and item.get("ctk_img"):
        return

    base_pil = item["base_pil"]
    normal_img = base_pil.convert("RGBA")
    ctk_img = ctk.CTkImage(
        light_image=normal_img,
        dark_image=normal_img,
        size=(int(theme.card_width), int(theme.card_height)),
    )
    image_refs.append(ctk_img)
    item["ctk_img"] = ctk_img
    item["ctk_img_cache_revision"] = base_revision
    item["current_image_state"] = None


def render_game_card_visual(
    item: MutableMapping[str, Any],
    *,
    selected: bool,
    hovered: bool,
    theme: GameCardVisualTheme,
    image_refs: list[Any],
) -> None:
    title_overlay = item["hover_title"]

    if selected or hovered:
        title_overlay.place(x=0, y=int(theme.title_overlay_y))
        title_overlay.lift()
    else:
        title_overlay.place_forget()

    render_game_card_status_badge(item)

    ensure_game_card_image_cache(
        item,
        theme=theme,
        image_refs=image_refs,
    )
    if item.get("current_image_state") == "normal":
        return

    item["img_label"].configure(image=item["ctk_img"])
    item["current_image_state"] = "normal"


def update_game_card_base_image(
    item: MutableMapping[str, Any],
    *,
    label: Any,
    pil_img: Any,
) -> bool:
    if item.get("img_label") is not label:
        return False

    item["base_pil"] = pil_img.convert("RGBA")
    item["base_revision"] = int(item.get("base_revision", 0)) + 1
    item["ctk_img"] = None
    item["ctk_img_cache_revision"] = -1
    item["current_image_state"] = None
    return True


__all__ = [
    "GameCardVisualTheme",
    "ensure_game_card_image_cache",
    "render_game_card_status_badge",
    "render_game_card_visual",
    "update_game_card_base_image",
]

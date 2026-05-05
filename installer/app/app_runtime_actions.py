from __future__ import annotations

import logging
from pathlib import Path
from typing import Any

from . import device_identity
from .install_selection_controller import InstallSelectionUiState
from .scan_entry_controller import ScanEntryState


def format_gpu_label_text(app: Any, gpu_info: str) -> str:
    normalized_gpu = str(gpu_info or "").strip() or app.txt.main.unknown_gpu
    return normalized_gpu


def _resolve_device_logo_image(app: Any, logo_key: str):
    normalized_key = str(logo_key or "").strip().lower()
    if not normalized_key:
        return None

    cache = getattr(app, "_device_logo_images", None)
    if cache is None:
        cache = {}
        app._device_logo_images = cache
    if normalized_key in cache:
        return cache[normalized_key]

    try:
        import customtkinter as ctk
        from PIL import Image
    except Exception:
        return None

    assets_dir = Path(getattr(getattr(app, "_app_paths", None), "assets_dir", ""))
    logo_path = assets_dir / "logos" / f"{normalized_key}.png"
    if not logo_path.exists():
        cache[normalized_key] = None
        return None

    try:
        pil_image = Image.open(logo_path).convert("RGBA")
        alpha_channel = pil_image.getchannel("A")
        trim_box = alpha_channel.getbbox()
        if trim_box:
            pil_image = pil_image.crop(trim_box)

        target_height = 36
        max_width = 72
        width, height = pil_image.size
        if width > 0 and height > 0:
            target_width = max(1, int(round(width * (target_height / float(height)))))
        else:
            target_width = target_height

        if target_width > max_width and target_width > 0:
            scale = max_width / float(target_width)
            target_width = max_width
            target_height = max(1, int(round(target_height * scale)))

        ctk_image = ctk.CTkImage(
            light_image=pil_image,
            dark_image=pil_image,
            size=(target_width, target_height),
        )
    except Exception:
        logging.getLogger().debug("Failed to load device logo from %s", logo_path, exc_info=True)
        cache[normalized_key] = None
        return None

    cache[normalized_key] = ctk_image
    return ctk_image


def refresh_device_info_header(app: Any) -> None:
    title_widget = getattr(app, "device_title_lbl", None)
    gpu_widget = getattr(app, "device_gpu_lbl", None)
    logo_widget = getattr(app, "device_logo_lbl", None)
    if title_widget is None or gpu_widget is None or logo_widget is None:
        return
    if hasattr(title_widget, "winfo_exists") and callable(title_widget.winfo_exists) and not title_widget.winfo_exists():
        return

    rules = getattr(app, "_device_identity_rules", device_identity.DeviceIdentityRules())
    device_info = getattr(getattr(app, "gpu_state", None), "device_info", None)
    raw_manufacturer = str(getattr(device_info, "manufacturer", "") or "").strip()
    raw_model = str(getattr(device_info, "model", "") or "").strip()

    title_text = device_identity.build_device_title(
        raw_manufacturer,
        raw_model,
        rules,
    )
    gpu_text = format_gpu_label_text(app, getattr(getattr(app, "gpu_state", None), "gpu_info", ""))

    title_widget.configure(text=title_text)
    gpu_widget.configure(text=gpu_text)

    logo_key = device_identity.resolve_device_logo_key(raw_manufacturer, rules)
    if not logo_key:
        selected_adapter = getattr(getattr(app, "gpu_state", None), "selected_adapter", None)
        selected_vendor = str(getattr(selected_adapter, "vendor", "") or "").strip()
        if not selected_vendor:
            gpu_context = getattr(getattr(app, "gpu_state", None), "gpu_context", None)
            selected_vendor = str(getattr(gpu_context, "selected_vendor", "") or "").strip()
        logo_key = device_identity.resolve_gpu_vendor_logo_key(selected_vendor)

    logo_image = _resolve_device_logo_image(app, logo_key)
    if logo_image is None:
        logo_widget.configure(image=None, text="", width=0)
        logo_widget.grid_remove()
        return

    logo_width = int(getattr(logo_image, "_size", (36, 36))[0]) if getattr(logo_image, "_size", None) else 36
    logo_widget.configure(image=logo_image, text="", width=logo_width)
    logo_widget.grid()


def set_gpu_label_text(app: Any, text: str) -> None:
    widget = getattr(app, "device_gpu_lbl", None)
    if widget is None:
        return
    if hasattr(widget, "winfo_exists") and callable(widget.winfo_exists) and not widget.winfo_exists():
        return
    widget.configure(text=str(text or ""))
    refresh_device_info_header(app)


def set_folder_select_enabled(app: Any, enabled: bool) -> None:
    widget = getattr(app, "btn_select_folder", None)
    if widget is None:
        return
    if hasattr(widget, "winfo_exists") and callable(widget.winfo_exists) and not widget.winfo_exists():
        return
    widget.configure(state="normal" if enabled else "disabled")


def request_close(app: Any) -> None:
    controller = getattr(app, "_app_actions_controller", None)
    if controller is None:
        return
    controller.request_close(bool(app.install_state.in_progress))


def shutdown_app(app: Any) -> None:
    controller = getattr(app, "_app_shutdown_controller", None)
    if controller is None:
        return
    controller.shutdown()


def start_game_db_load_async(app: Any) -> None:
    controller = getattr(app, "_game_db_controller", None)
    if controller is None:
        return

    sheet_state = app.sheet_state
    game_db_vendor = str(sheet_state.active_vendor or "default")
    gpu_model = str(getattr(app.gpu_state, "gpu_info", "") or "").strip()
    started = controller.start_load(game_db_vendor, gpu_model)
    if not started:
        return
    logging.info(
        "[APP] Starting Game DB load for vendor=%s gpu=%s",
        game_db_vendor,
        app.gpu_state.gpu_info,
    )


def is_scan_in_progress(app: Any) -> bool:
    controller = getattr(app, "_scan_controller", None)
    return bool(controller and controller.is_scan_in_progress)


def clear_found_games(app: Any) -> None:
    app.found_exe_list = []


def pump_poster_queue(app: Any) -> None:
    app._poster_queue.pump()


def start_auto_scan(app: Any) -> None:
    if app.gpu_state.multi_gpu_blocked:
        return
    if app.install_state.in_progress:
        return
    controller = getattr(app, "_scan_controller", None)
    if controller is None:
        return
    controller.start_auto_scan()


def set_game_folder(app: Any, folder_path: str) -> None:
    app.game_folder = str(folder_path or "")


def start_manual_scan_from_folder(app: Any, folder_path: str) -> bool:
    controller = getattr(app, "_scan_controller", None)
    if controller is None:
        return False
    if app.install_state.in_progress:
        return False
    return controller.start_manual_scan(folder_path)


def apply_install_selection_state(app: Any, state: InstallSelectionUiState) -> None:
    install_state = app.install_state
    install_state.popup_confirmed = bool(state.popup_confirmed)
    install_state.precheck_running = bool(state.precheck_running)
    install_state.precheck_ok = bool(state.precheck_ok)
    install_state.precheck_error = str(state.precheck_error or "")
    install_state.precheck_dll_name = str(state.precheck_dll_name or "")


def build_reset_install_selection_ui_state(
    *,
    precheck_error: str = "",
    precheck_dll_name: str = "",
) -> InstallSelectionUiState:
    return InstallSelectionUiState(
        popup_confirmed=False,
        precheck_running=False,
        precheck_ok=False,
        precheck_error=str(precheck_error or ""),
        precheck_dll_name=str(precheck_dll_name or ""),
    )


def reset_install_selection_state(
    app: Any,
    *,
    precheck_error: str = "",
    precheck_dll_name: str = "",
) -> None:
    apply_install_selection_state(
        app,
        build_reset_install_selection_ui_state(
            precheck_error=precheck_error,
            precheck_dll_name=precheck_dll_name,
        ),
    )


def build_scan_entry_state(app: Any) -> ScanEntryState:
    gpu_state = app.gpu_state
    sheet_state = app.sheet_state
    return ScanEntryState(
        multi_gpu_blocked=bool(gpu_state.multi_gpu_blocked),
        sheet_loading=bool(sheet_state.loading),
        sheet_ready=bool(sheet_state.status),
    )


def select_game_folder(app: Any) -> None:
    controller = getattr(app, "_scan_entry_controller", None)
    if controller is None:
        return
    if app.install_state.in_progress:
        return
    controller.select_game_folder(build_scan_entry_state(app))


def apply_selected_install(app: Any):
    controller = getattr(app, "_install_flow_controller", None)
    if controller is None:
        return None
    return controller.apply_selected_install()


__all__ = [
    "apply_install_selection_state",
    "apply_selected_install",
    "build_reset_install_selection_ui_state",
    "build_scan_entry_state",
    "clear_found_games",
    "format_gpu_label_text",
    "is_scan_in_progress",
    "pump_poster_queue",
    "refresh_device_info_header",
    "request_close",
    "reset_install_selection_state",
    "select_game_folder",
    "set_folder_select_enabled",
    "set_game_folder",
    "set_gpu_label_text",
    "shutdown_app",
    "start_auto_scan",
    "start_game_db_load_async",
    "start_manual_scan_from_folder",
]

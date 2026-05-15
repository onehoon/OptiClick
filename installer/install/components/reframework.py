from __future__ import annotations

from typing import Mapping

from ._link_utils import extract_module_url
from .dll_payload import install_dll_payload_from_archive

REFRAMEWORK_SOURCE_DLL_NAME = "dinput8.dll"


def install_reframework_dinput8(
    target_path: str,
    game_data: Mapping[str, object],
    module_download_links: Mapping[str, object],
    logger=None,
    cached_archive_path: str = "",
) -> bool:
    destination_rel_path = str(game_data.get("reframework_url", "") or "").strip()
    if not destination_rel_path:
        return False
    url = extract_module_url(module_download_links, "reframework")
    if not cached_archive_path and not url:
        raise FileNotFoundError("REFramework download link is not configured")
    install_dll_payload_from_archive(
        target_path=target_path,
        destination_rel_path=destination_rel_path,
        source_dll_name=REFRAMEWORK_SOURCE_DLL_NAME,
        url=url,
        cached_archive_path=cached_archive_path,
        logger=logger,
        temp_prefix=".opticlick_reframework_tmp_",
    )
    if logger:
        logger.info("Installed REFramework to %s", destination_rel_path)
    return True

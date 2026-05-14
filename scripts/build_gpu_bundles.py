from __future__ import annotations

import argparse
import json
import os
import re
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from google.oauth2 import service_account
from googleapiclient.discovery import build


SCOPES = ["https://www.googleapis.com/auth/spreadsheets.readonly"]
SHEET_MANIFEST = "gpu_bundle_manifest"
SHEET_GROUP = "gpu_bundle_group"
SHEET_INI = "optiscaler_ini_profile"
VALID_VENDORS = {"amd", "nvidia", "intel"}
VALID_MATCH_MODES = {"exact", "contains"}
KEY_PATTERN = re.compile(r"^[a-z0-9_]+$")


@dataclass(frozen=True)
class ManifestRule:
    enabled: bool
    vendor: str
    match_mode: str
    match_value: str
    bundle_key: str
    gpu_group: str
    display_name: str
    priority: int
    memo: str
    row_order: int


@dataclass(frozen=True)
class GroupRow:
    enabled: bool
    profile_id: str
    game_id: str
    vendor: str
    gpu_group: str
    priority: int
    optiscaler_dll_name: str
    ultimate_asi_loader: bool
    optipatcher: bool
    specialk: bool
    reframework_url: str
    unreal5: bool
    rtss_overlay: bool
    extra_bundle: str
    memo: str
    row_order: int


@dataclass(frozen=True)
class IniRow:
    profile_id: str
    section: str
    key: str
    value: str
    priority: int
    enabled: bool
    memo: str
    row_order: int


def normalize_text(value: Any) -> str:
    if value is None:
        return ""
    return str(value).strip()


def normalize_header(value: Any) -> str:
    return re.sub(r"\s+", "_", normalize_text(value).lower())


def parse_bool(value: Any, *, default: bool = False) -> bool:
    if value is None:
        return default
    text = normalize_text(value).lower()
    if not text:
        return default
    if text in {"1", "true", "t", "yes", "y", "on"}:
        return True
    if text in {"0", "false", "f", "no", "n", "off"}:
        return False
    return default


def parse_int(value: Any, *, default: int = 100) -> int:
    text = normalize_text(value)
    if not text:
        return default
    try:
        return int(float(text))
    except ValueError:
        return default


def escape_sheet_name_for_a1(sheet_name: str) -> str:
    escaped = sheet_name.replace("'", "''")
    return f"'{escaped}'"


def build_rows(values: list[list[Any]], *, sheet_name: str) -> list[dict[str, Any]]:
    if not values:
        raise ValueError(f"{sheet_name}: empty sheet")

    headers = [normalize_header(v) for v in values[0]]
    if not any(headers):
        raise ValueError(f"{sheet_name}: header row is empty")

    rows: list[dict[str, Any]] = []
    width = len(headers)
    for index, raw_row in enumerate(values[1:], start=2):
        row = list(raw_row[:width]) + [""] * max(0, width - len(raw_row))
        if all(normalize_text(cell) == "" for cell in row):
            continue
        item: dict[str, Any] = {"__row_order__": index}
        for col_index, header in enumerate(headers):
            if not header:
                continue
            item[header] = row[col_index]
        rows.append(item)
    return rows


def fetch_sheet_rows(
    *,
    credentials_path: str,
    spreadsheet_id: str,
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], list[dict[str, Any]]]:
    credentials = service_account.Credentials.from_service_account_file(
        credentials_path,
        scopes=SCOPES,
    )
    service = build("sheets", "v4", credentials=credentials, cache_discovery=False)

    ranges = [
        escape_sheet_name_for_a1(SHEET_MANIFEST),
        escape_sheet_name_for_a1(SHEET_GROUP),
        escape_sheet_name_for_a1(SHEET_INI),
    ]
    response = (
        service.spreadsheets()
        .values()
        .batchGet(
            spreadsheetId=spreadsheet_id,
            ranges=ranges,
            majorDimension="ROWS",
            valueRenderOption="UNFORMATTED_VALUE",
            dateTimeRenderOption="FORMATTED_STRING",
        )
        .execute()
    )

    value_ranges = response.get("valueRanges", [])
    if len(value_ranges) != 3:
        raise ValueError("Failed to read required sheets: gpu_bundle_manifest, gpu_bundle_group, optiscaler_ini_profile")

    manifest_rows = build_rows(value_ranges[0].get("values", []), sheet_name=SHEET_MANIFEST)
    group_rows = build_rows(value_ranges[1].get("values", []), sheet_name=SHEET_GROUP)
    ini_rows = build_rows(value_ranges[2].get("values", []), sheet_name=SHEET_INI)
    return manifest_rows, group_rows, ini_rows


def parse_manifest_rules(rows: list[dict[str, Any]]) -> list[ManifestRule]:
    errors: list[str] = []
    rules: list[ManifestRule] = []
    seen_bundle_group: dict[tuple[str, str], str] = {}

    for row in rows:
        if not parse_bool(row.get("enabled"), default=False):
            continue
        vendor = normalize_text(row.get("vendor")).lower()
        match_mode = normalize_text(row.get("match_mode")).lower()
        match_value = normalize_text(row.get("match_value"))
        bundle_key = normalize_text(row.get("bundle_key")).lower()
        gpu_group = normalize_text(row.get("gpu_group")).lower()
        display_name = normalize_text(row.get("display_name"))
        memo = normalize_text(row.get("memo"))
        priority = parse_int(row.get("priority"), default=100)
        row_order = int(row.get("__row_order__", 0))

        prefix = f"{SHEET_MANIFEST} row {row_order}"
        if vendor not in VALID_VENDORS:
            errors.append(f"{prefix}: invalid vendor '{vendor}'")
        if match_mode not in VALID_MATCH_MODES:
            errors.append(f"{prefix}: invalid match_mode '{match_mode}'")
        if not match_value:
            errors.append(f"{prefix}: match_value is required")
        if not bundle_key:
            errors.append(f"{prefix}: bundle_key is required")
        if not gpu_group:
            errors.append(f"{prefix}: gpu_group is required")
        if bundle_key and not KEY_PATTERN.fullmatch(bundle_key):
            errors.append(f"{prefix}: bundle_key must match ^[a-z0-9_]+$")
        if gpu_group and not KEY_PATTERN.fullmatch(gpu_group):
            errors.append(f"{prefix}: gpu_group must match ^[a-z0-9_]+$")

        key = (vendor, bundle_key)
        if key in seen_bundle_group and seen_bundle_group[key] != gpu_group:
            errors.append(
                f"{prefix}: vendor+bundle_key '{vendor}/{bundle_key}' has inconsistent gpu_group "
                f"('{seen_bundle_group[key]}' vs '{gpu_group}')"
            )
        seen_bundle_group[key] = gpu_group

        rules.append(
            ManifestRule(
                enabled=True,
                vendor=vendor,
                match_mode=match_mode,
                match_value=match_value,
                bundle_key=bundle_key,
                gpu_group=gpu_group,
                display_name=display_name,
                priority=priority,
                memo=memo,
                row_order=row_order,
            )
        )

    if errors:
        raise ValueError("\n".join(errors))
    if not rules:
        raise ValueError("gpu_bundle_manifest has no enabled rules")
    return sorted(rules, key=lambda item: (item.priority, item.row_order, item.vendor, item.bundle_key))


def parse_group_rows(rows: list[dict[str, Any]]) -> list[GroupRow]:
    errors: list[str] = []
    entries: list[GroupRow] = []

    for row in rows:
        if not parse_bool(row.get("enabled"), default=False):
            continue
        profile_id = normalize_text(row.get("profile_id"))
        game_id = normalize_text(row.get("game_id"))
        vendor = normalize_text(row.get("vendor")).lower()
        gpu_group = normalize_text(row.get("gpu_group")).lower()
        priority = parse_int(row.get("priority"), default=100)
        row_order = int(row.get("__row_order__", 0))
        prefix = f"{SHEET_GROUP} row {row_order}"

        if not profile_id:
            errors.append(f"{prefix}: profile_id is required")
        if not game_id:
            errors.append(f"{prefix}: game_id is required")
        if vendor not in VALID_VENDORS:
            errors.append(f"{prefix}: invalid vendor '{vendor}'")
        if gpu_group and not KEY_PATTERN.fullmatch(gpu_group):
            errors.append(f"{prefix}: gpu_group must match ^[a-z0-9_]+$")

        entries.append(
            GroupRow(
                enabled=True,
                profile_id=profile_id,
                game_id=game_id,
                vendor=vendor,
                gpu_group=gpu_group,
                priority=priority,
                optiscaler_dll_name=normalize_text(row.get("optiscaler_dll_name")),
                ultimate_asi_loader=parse_bool(row.get("ultimate_asi_loader"), default=False),
                optipatcher=parse_bool(row.get("optipatcher"), default=False),
                specialk=parse_bool(row.get("specialk"), default=False),
                reframework_url=normalize_text(row.get("reframework_url")),
                unreal5=parse_bool(row.get("unreal5"), default=False),
                rtss_overlay=parse_bool(row.get("rtss_overlay"), default=False),
                extra_bundle=normalize_text(row.get("extra_bundle")),
                memo=normalize_text(row.get("memo")),
                row_order=row_order,
            )
        )

    if errors:
        raise ValueError("\n".join(errors))
    return entries


def parse_ini_rows(rows: list[dict[str, Any]]) -> list[IniRow]:
    parsed: list[IniRow] = []
    for row in rows:
        enabled = parse_bool(row.get("enabled"), default=False)
        if not enabled:
            continue
        profile_id = normalize_text(row.get("profile_id"))
        if not profile_id:
            continue
        value = row.get("value")
        if isinstance(value, bool):
            normalized_value = "true" if value else "false"
        elif value is None:
            normalized_value = ""
        else:
            normalized_value = str(value)
        parsed.append(
            IniRow(
                profile_id=profile_id,
                section=normalize_text(row.get("section")),
                key=normalize_text(row.get("key")),
                value=normalized_value,
                priority=parse_int(row.get("priority"), default=100),
                enabled=True,
                memo=normalize_text(row.get("memo")),
                row_order=int(row.get("__row_order__", 0)),
            )
        )
    return parsed


def unique_bundle_pairs(rules: list[ManifestRule]) -> list[tuple[str, str, str]]:
    pairs: list[tuple[str, str, str]] = []
    seen: set[tuple[str, str]] = set()
    for rule in rules:
        key = (rule.vendor, rule.bundle_key)
        if key in seen:
            continue
        seen.add(key)
        pairs.append((rule.vendor, rule.bundle_key, rule.gpu_group))
    return pairs


def select_game_profiles(entries: list[GroupRow], *, target_gpu_group: str) -> list[GroupRow]:
    selected: dict[str, GroupRow] = {}
    for entry in sorted(
        entries,
        key=lambda item: (
            0 if item.gpu_group == target_gpu_group else 1,
            item.priority,
            item.row_order,
        ),
    ):
        key = entry.game_id.casefold()
        if key not in selected:
            selected[key] = entry
    return sorted(selected.values(), key=lambda item: item.row_order)


def build_active_profile_ids(vendor: str, selected_games: list[GroupRow]) -> set[str]:
    active = {"GLOBAL_ALL", f"GLOBAL_{vendor.upper()}"}
    for game in selected_games:
        active.add(f"{game.game_id}_ALL")
        active.add(game.profile_id)
    return active


def filter_ini_rows(rows: list[IniRow], active_profile_ids: set[str]) -> list[dict[str, Any]]:
    normalized = {item.casefold() for item in active_profile_ids}
    matched = [
        row
        for row in rows
        if row.enabled and row.profile_id.casefold() in normalized
    ]
    matched.sort(key=lambda item: (item.priority, item.row_order))
    return [
        {
            "profile_id": row.profile_id,
            "section": row.section,
            "key": row.key,
            "value": row.value,
            "priority": row.priority,
            "enabled": True,
            "memo": row.memo,
        }
        for row in matched
    ]


def build_bundle_payload(
    *,
    vendor: str,
    bundle_key: str,
    gpu_group: str,
    group_rows: list[GroupRow],
    ini_rows: list[IniRow],
    generated_at: str,
) -> dict[str, Any]:
    candidate_rows = [
        row
        for row in group_rows
        if row.enabled and row.vendor == vendor and (row.gpu_group == gpu_group or row.gpu_group == "")
    ]
    if not candidate_rows:
        raise ValueError(
            f"Bundle {vendor}/{bundle_key} has no enabled gpu_bundle_group rows for gpu_group '{gpu_group}'"
        )

    selected_games = select_game_profiles(candidate_rows, target_gpu_group=gpu_group)
    if not selected_games:
        raise ValueError(f"Bundle {vendor}/{bundle_key} resolved to zero games")

    games_payload = [
        {
            "game_id": row.game_id,
            "profile_id": row.profile_id,
            "install_profile": {
                "profile_id": row.profile_id,
                "optiscaler_dll_name": row.optiscaler_dll_name,
                "extra_bundle": row.extra_bundle,
                "ultimate_asi_loader": row.ultimate_asi_loader,
                "optipatcher": row.optipatcher,
                "specialk": row.specialk,
                "reframework_url": row.reframework_url,
                "unreal5": row.unreal5,
                "rtss_overlay": row.rtss_overlay,
                "enabled": True,
            },
        }
        for row in selected_games
    ]

    active_ids = build_active_profile_ids(vendor, selected_games)
    ini_payload = filter_ini_rows(ini_rows, active_ids)

    return {
        "ok": True,
        "schema_version": 1,
        "vendor": vendor,
        "bundle_key": bundle_key,
        "gpu_group": gpu_group,
        "generated_at": generated_at,
        "games": games_payload,
        "profiles": {
            "optiscaler_ini": ini_payload,
        },
    }


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as file:
        json.dump(payload, file, ensure_ascii=False, indent=2)
        file.write("\n")


def build_gpu_bundles(
    *,
    credentials_path: str,
    spreadsheet_id: str,
    output_dir: Path,
    manifest_version: int,
) -> None:
    manifest_sheet_rows, group_sheet_rows, ini_sheet_rows = fetch_sheet_rows(
        credentials_path=credentials_path,
        spreadsheet_id=spreadsheet_id,
    )
    manifest_rules = parse_manifest_rules(manifest_sheet_rows)
    group_rows = parse_group_rows(group_sheet_rows)
    ini_rows = parse_ini_rows(ini_sheet_rows)
    generated_at = datetime.now(timezone.utc).isoformat()

    output_dir.mkdir(parents=True, exist_ok=True)
    manifest_payload = {
        "schema_version": 1,
        "manifest_version": manifest_version,
        "generated_at": generated_at,
        "rules": [
            {
                "enabled": rule.enabled,
                "vendor": rule.vendor,
                "match_mode": rule.match_mode,
                "match_value": rule.match_value,
                "bundle_key": rule.bundle_key,
                "gpu_group": rule.gpu_group,
                "display_name": rule.display_name,
                "priority": rule.priority,
                "memo": rule.memo,
            }
            for rule in manifest_rules
        ],
    }
    write_json(output_dir / "manifest.json", manifest_payload)

    bundle_pairs = unique_bundle_pairs(manifest_rules)
    for vendor, bundle_key, gpu_group in bundle_pairs:
        bundle_payload = build_bundle_payload(
            vendor=vendor,
            bundle_key=bundle_key,
            gpu_group=gpu_group,
            group_rows=group_rows,
            ini_rows=ini_rows,
            generated_at=generated_at,
        )
        write_json(output_dir / vendor / f"{bundle_key}.json", bundle_payload)

    print(f"[OK] manifest rules: {len(manifest_rules)}")
    print(f"[OK] bundles generated: {len(bundle_pairs)}")
    print(f"[OK] output dir: {output_dir}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Build GPU bundle JSON files from Google Sheets.")
    parser.add_argument(
        "--spreadsheet-id",
        default=os.environ.get("GOOGLE_SPREADSHEET_ID", ""),
        help="Google Spreadsheet ID. Defaults to GOOGLE_SPREADSHEET_ID env.",
    )
    parser.add_argument(
        "--credentials",
        default=os.environ.get("GOOGLE_APPLICATION_CREDENTIALS", ""),
        help="Path to Google service account json. Defaults to GOOGLE_APPLICATION_CREDENTIALS env.",
    )
    parser.add_argument(
        "--output-dir",
        default=os.environ.get("GPU_BUNDLE_OUTPUT_DIR", "dist/gpu-bundles"),
        help="Output directory for generated bundle files.",
    )
    parser.add_argument(
        "--manifest-version",
        type=int,
        default=int(os.environ.get("GPU_BUNDLE_MANIFEST_VERSION", "0") or "0"),
        help="Manifest version integer. If 0, generated from current epoch seconds.",
    )
    args = parser.parse_args()

    spreadsheet_id = normalize_text(args.spreadsheet_id)
    credentials_path = normalize_text(args.credentials)
    if not spreadsheet_id:
        raise ValueError("GOOGLE_SPREADSHEET_ID (or --spreadsheet-id) is required")
    if not credentials_path:
        raise ValueError("GOOGLE_APPLICATION_CREDENTIALS (or --credentials) is required")

    manifest_version = int(args.manifest_version or 0)
    if manifest_version <= 0:
        manifest_version = int(time.time())

    build_gpu_bundles(
        credentials_path=credentials_path,
        spreadsheet_id=spreadsheet_id,
        output_dir=Path(args.output_dir),
        manifest_version=manifest_version,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

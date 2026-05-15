import json
import os
import subprocess
import tempfile
from datetime import date, timedelta
from pathlib import Path
from typing import Any
from urllib.parse import quote

import requests


ROOT_DIR = Path(__file__).resolve().parents[1]
GAME_MASTER_PATH = ROOT_DIR / "assets" / "data" / "game_master.json"
NEW_GAME_SUPPORT_JSON_PATH = ROOT_DIR / "assets" / "data" / "new_game_support.json"
WIKI_REPO_URL = "https://github.com/onehoon/OptiClick.wiki.git"
SHEET_GPU_BUNDLE_GROUP = "gpu_bundle_group"
INTEL_FULL_GROUPS = {"intel_mtl", "intel_lnl", "intel_ptl", "intel_a_series", "intel_b_series", "intel_pro_series"}
INTEL_NON_ARC_LABELS = {"intel_mtl": "Arc Graphics", "intel_lnl": "Lunar Lake", "intel_ptl": "Panther Lake"}
INTEL_ARC_SERIES_GROUP_LABELS = {"intel_a_series": "A", "intel_b_series": "B", "intel_pro_series": "Pro"}

NEW_GAMES_HEADING = "## 신규 지원 게임 추가 / Newly Supported Games"
NEW_GAMES_METADATA_START = "<!-- newly-supported-games"
NEW_GAMES_METADATA_END = "-->"
LEGACY_NEW_GAMES_TABLE_HEADER = "| Korean Title | English Title |"
NEW_GAMES_TABLE_HEADER = "| Korean Title | English Title | Intel | AMD | NVIDIA |"
NEW_GAMES_TABLE_SEPARATOR = "|---|---|---|---|---|"
NEW_GAMES_HEADING_ALIASES = {
    NEW_GAMES_HEADING,
    "## 신규 지원 게임 추가 / Newly Added Supported Games",
}

SUPPORTED_GAMES_TABLE_HEADER = "| Korean Title | English Title | Intel | AMD | NVIDIA |"
SUPPORTED_GAMES_TABLE_SEPARATOR = "|---|---|---|---|---|"

WIKI_CAUTION_BLOCKS = [
    "> [!CAUTION]",
    "> 각종 MOD 사용 시 게임 실행이 불가능하거나 호환되지 않을 수 있습니다.",
    "> - ReShade, Special K, RenoDX 등",
    "> - 이 경우 OptiScaler를 수동으로 직접 설치하시기 바랍니다.",
    "> 게임 성능은 GPU 모델 및 게임 옵션에 따라 달라질 수 있습니다.",
    "> 사용하는 PC 환경에 따라 OptiScaler가 정상적으로 동작하지 않을 수 있습니다.",
    "",
    "> [!CAUTION]",
    "> Using certain mods may prevent the game from launching or cause compatibility issues.  ",
    "> - ReShade, Special K, RenoDX etc.",
    "> - In this case, please install OptiScaler manually.",
    "> Game performance may vary depending on your GPU model and game options.",
    "> OptiScaler may not work properly depending on your PC environment.",
]

RADEON_IGPU_NOTE_BLOCKS = [
    "> [!NOTE]",
    "> AMD Radeon iGPU* in this list refers to Radeon 780M, 880M, 890M, 8050S, and 8060S.",
    "> 이 리스트에서 AMD Radeon iGPU* 지원은 Radeon 780M, 880M, 890M, 8050S, 8060S를 의미합니다.",
]

WIKI_PUSH_TOKEN = str(os.environ.get("WIKI_PUSH_TOKEN", "") or "").strip()
TARGET_WIKI_PAGE_FILE = str(
    os.environ.get("TARGET_WIKI_PAGE_FILE", "Supported-Game-List.md") or ""
).strip()
BASELINE_WIKI_PAGE_FILE = str(
    os.environ.get("BASELINE_WIKI_PAGE_FILE", "Supported-Game-List.md") or ""
).strip()


def require_env_value(name: str) -> str:
    value = str(os.environ.get(name, "") or "").strip()
    if not value:
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def require_int_env_value(name: str, default: int) -> int:
    raw = str(os.environ.get(name, "") or "").strip()
    if not raw:
        return int(default)
    try:
        return int(raw)
    except ValueError as exc:
        raise RuntimeError(f"Environment variable {name} must be an integer.") from exc


def normalize_text(value: Any) -> str:
    if value is None:
        return ""
    if value != value:
        return ""
    return str(value).strip()


def normalize_header(value: Any) -> str:
    return normalize_text(value).lstrip("\ufeff").strip().lower()


def escape_md(text: Any) -> str:
    return normalize_text(text).replace("|", "\\|").replace("\n", " ")


def unescape_md_table_cell(text: Any) -> str:
    return normalize_text(text).replace("\\|", "|")


def parse_bool(value: Any, default: bool = False) -> bool:
    normalized = normalize_text(value).lower()
    if not normalized:
        return bool(default)
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    return bool(default)


def is_new_games_heading(line: str) -> bool:
    return normalize_text(line) in NEW_GAMES_HEADING_ALIASES


def build_google_session() -> requests.Session:
    from google.auth.transport.requests import Request
    from google.oauth2.service_account import Credentials

    service_account_info = json.loads(require_env_value("GCP_SERVICE_ACCOUNT_KEY"))
    credentials = Credentials.from_service_account_info(
        service_account_info,
        scopes=["https://www.googleapis.com/auth/spreadsheets.readonly"],
    )
    credentials.refresh(Request())

    session = requests.Session()
    session.headers.update({"Authorization": f"Bearer {credentials.token}"})
    return session


def fetch_sheet_rows(session: requests.Session, spreadsheet_id: str, sheet_title: str) -> list[dict[str, str]]:
    quoted_title = sheet_title.replace("'", "''")
    range_expr = f"'{quoted_title}'"
    encoded_range = quote(range_expr, safe="")
    response = session.get(
        f"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheet_id}/values/{encoded_range}",
        timeout=30,
    )
    response.raise_for_status()
    payload = response.json()
    values = payload.get("values") or []
    if not values:
        return []

    headers = [normalize_header(cell) for cell in values[0]]
    rows: list[dict[str, str]] = []
    for raw_row in values[1:]:
        row = {
            headers[index]: normalize_text(raw_row[index]) if index < len(raw_row) else ""
            for index in range(len(headers))
            if headers[index]
        }
        if any(normalize_text(value) for value in row.values()):
            rows.append(row)
    return rows


def resolve_sheet_title(session: requests.Session, spreadsheet_id: str, requested_title: str) -> str:
    response = session.get(
        f"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheet_id}",
        params={"fields": "sheets(properties(title))"},
        timeout=30,
    )
    response.raise_for_status()
    payload = response.json()

    requested_key = normalize_text(requested_title).lower()
    available_titles: list[str] = []
    for sheet in payload.get("sheets", []):
        properties = sheet.get("properties") or {}
        title = normalize_text(properties.get("title"))
        if not title:
            continue
        available_titles.append(title)
        if title.lower() == requested_key:
            return title

    raise RuntimeError(
        f"Sheet tab not found: {requested_title}. Available tabs: {', '.join(available_titles)}"
    )


def is_test_sheet_row(row: dict[str, str]) -> bool:
    targets = (
        normalize_text(row.get("profile_id")),
        normalize_text(row.get("game_id")),
    )
    return any("test" in target.lower() for target in targets if target)


def is_test_game_title(*titles: Any) -> bool:
    keywords = ("test", "테스트")
    for title in titles:
        text = normalize_text(title).lower()
        if text and any(keyword in text for keyword in keywords):
            return True
    return False


def normalize_vendor(value: Any) -> str:
    text = normalize_text(value).upper()
    if text in {"INTEL", "AMD", "NVIDIA"}:
        return text
    return ""


def load_gpu_bundle_group_rows() -> list[dict[str, str]]:
    spreadsheet_id = require_env_value("GOOGLE_SPREADSHEET_ID")
    session = build_google_session()
    resolved_title = resolve_sheet_title(session, spreadsheet_id, SHEET_GPU_BUNDLE_GROUP)
    return fetch_sheet_rows(session, spreadsheet_id, resolved_title)


def load_game_master() -> dict[str, dict[str, Any]]:
    payload = json.loads(GAME_MASTER_PATH.read_text(encoding="utf-8"))
    games: dict[str, dict[str, Any]] = {}
    for raw_game in payload:
        if not isinstance(raw_game, dict):
            continue
        if not parse_bool(raw_game.get("enabled"), default=False):
            continue
        game_id = normalize_text(raw_game.get("game_id"))
        if not game_id:
            continue
        if "_" in game_id:
            raise RuntimeError(
                "game_master.json game_id values must not contain '_' because "
                f"profile _all fallback splitting depends on that invariant: {game_id}"
            )
        if is_test_game_title(raw_game.get("game_name_kr"), raw_game.get("game_name_en")):
            continue
        games[game_id] = dict(raw_game)
    return games


def build_sheet_index(rows: list[dict[str, str]]) -> dict[str, dict[str, list[str]]]:
    indexed: dict[str, dict[str, list[str]]] = {}

    for row in rows:
        if is_test_sheet_row(row):
            continue
        if not parse_bool(row.get("enabled"), default=True):
            continue

        game_id = normalize_text(row.get("game_id"))
        if not game_id:
            continue

        vendor = normalize_vendor(row.get("vendor"))
        if not vendor:
            print(f"[WARN] Skipping row with unknown vendor for game_id={game_id}")
            continue

        gpu_group = normalize_text(row.get("gpu_group")).lower()
        if not gpu_group:
            continue

        vendor_map = indexed.setdefault(game_id, {"INTEL": [], "AMD": [], "NVIDIA": []})
        if gpu_group not in vendor_map[vendor]:
            vendor_map[vendor].append(gpu_group)

    return indexed


def format_intel_groups(groups: list[str]) -> str:
    normalized = {normalize_text(group).lower() for group in groups if normalize_text(group)}
    if not normalized:
        return "Not Supported"
    if "all" in normalized or INTEL_FULL_GROUPS.issubset(normalized):
        return "Intel Arc Series"
    labels: list[str] = []
    for group in ["intel_mtl", "intel_lnl", "intel_ptl"]:
        if group in normalized:
            label = INTEL_NON_ARC_LABELS[group]
            if label not in labels:
                labels.append(label)
    arc_parts: list[str] = []
    for group in ["intel_a_series", "intel_b_series", "intel_pro_series"]:
        if group in normalized:
            arc_parts.append(INTEL_ARC_SERIES_GROUP_LABELS[group])
    if arc_parts:
        labels.append("Arc " + "/".join(arc_parts) + " Series")
    for group in sorted(normalized - (INTEL_FULL_GROUPS | {"all"})):
        print(f"[WARN] Unknown Intel gpu_group: {group}")
    if not labels:
        return "Supported"
    return "Intel " + ", ".join(labels)


def format_amd_groups(groups: list[str]) -> str:
    normalized = {normalize_text(group).lower() for group in groups if normalize_text(group)}
    if not normalized:
        return "Not Supported"
    if "all" in normalized:
        normalized.update({"radeon_igpu", "radeon_rx60_70", "radeon_rx90"})
    labels: list[str] = []
    if "radeon_igpu" in normalized:
        labels.append("Radeon iGPU*")
    has_rx60_70 = "radeon_rx60_70" in normalized
    has_rx90 = "radeon_rx90" in normalized
    if has_rx60_70 and has_rx90:
        labels.append("RX 6/7/9000 Series")
    elif has_rx60_70:
        labels.append("RX 6000/7000 Series")
    elif has_rx90:
        labels.append("RX 9000 Series")
    for group in sorted(normalized - {"radeon_igpu", "radeon_rx60_70", "radeon_rx90", "all"}):
        print(f"[WARN] Unknown AMD gpu_group: {group}")
    if not labels:
        return "Supported"
    return ", ".join(labels)


def format_nvidia_groups(groups: list[str]) -> str:
    normalized = {normalize_text(group).lower() for group in groups if normalize_text(group)}
    if not normalized:
        return "Not Supported"
    if "all" in normalized:
        normalized.update({"rtx_2030", "rtx_4050"})
    has_2030 = "rtx_2030" in normalized
    has_4050 = "rtx_4050" in normalized
    if has_2030 and has_4050:
        return "RTX 20/30/40/50 Series"
    if has_2030:
        return "RTX 20/30 Series"
    if has_4050:
        return "RTX 40/50 Series"
    for group in sorted(normalized - {"rtx_2030", "rtx_4050", "all"}):
        print(f"[WARN] Unknown NVIDIA gpu_group: {group}")
    return "Supported"


def build_vendor_display(
    game: dict[str, Any],
    sheet_vendor_groups: list[str],
    *,
    vendor: str,
) -> str:
    _ = game
    vendor_key = normalize_vendor(vendor)
    if vendor_key == "INTEL":
        return format_intel_groups(sheet_vendor_groups)
    if vendor_key == "AMD":
        return format_amd_groups(sheet_vendor_groups)
    if vendor_key == "NVIDIA":
        return format_nvidia_groups(sheet_vendor_groups)
    return "Not Supported"


def build_games() -> list[dict[str, str]]:
    game_master = load_game_master()
    gpu_bundle_group_rows = load_gpu_bundle_group_rows()
    sheet_index = build_sheet_index(gpu_bundle_group_rows)

    unknown_sheet_games = sorted(game_id for game_id in sheet_index if game_id not in game_master)
    for game_id in unknown_sheet_games:
        print(f"[WARN] gpu_bundle_group references unknown game_id in game_master.json: {game_id}")

    games: list[dict[str, str]] = []
    for game_id, game in game_master.items():
        vendor_groups = sheet_index.get(game_id, {"INTEL": [], "AMD": [], "NVIDIA": []})
        intel = build_vendor_display(game, vendor_groups["INTEL"], vendor="INTEL")
        amd = build_vendor_display(game, vendor_groups["AMD"], vendor="AMD")
        nvidia = build_vendor_display(game, vendor_groups["NVIDIA"], vendor="NVIDIA")

        if all(display == "Not Supported" for display in (intel, amd, nvidia)):
            continue

        games.append(
            {
                "game_name_kr": normalize_text(game.get("game_name_kr")),
                "game_name_en": normalize_text(game.get("game_name_en")),
                "Intel": intel,
                "AMD": amd,
                "NVIDIA": nvidia,
            }
        )

    games.sort(
        key=lambda item: (
            normalize_text(item.get("game_name_kr")).lower(),
            normalize_text(item.get("game_name_en")).lower(),
        )
    )
    return games


def build_markdown(games: list[dict[str, str]]) -> str:
    lines: list[str] = []
    lines.extend(WIKI_CAUTION_BLOCKS)
    lines.append("")
    lines.extend(RADEON_IGPU_NOTE_BLOCKS)
    lines.append("")
    lines.append(SUPPORTED_GAMES_TABLE_HEADER)
    lines.append(SUPPORTED_GAMES_TABLE_SEPARATOR)

    for game in games:
        lines.append(
            f"| {escape_md(game['game_name_kr'])} | "
            f"{escape_md(game['game_name_en'])} | "
            f"{escape_md(game['Intel'])} | "
            f"{escape_md(game['AMD'])} | "
            f"{escape_md(game['NVIDIA'])} |"
        )

    lines.append("")
    return "\n".join(lines)


def split_markdown_table_row(line: str) -> list[str]:
    text = normalize_text(line)
    if not text.startswith("|") or not text.endswith("|"):
        return []

    cells: list[str] = []
    current: list[str] = []
    escaped = False

    for char in text[1:-1]:
        if escaped:
            current.append(char)
            escaped = False
            continue
        if char == "\\":
            current.append(char)
            escaped = True
            continue
        if char == "|":
            cells.append(unescape_md_table_cell("".join(current)))
            current = []
            continue
        current.append(char)

    cells.append(unescape_md_table_cell("".join(current)))
    return cells


def make_game_identity(game: dict[str, str]) -> tuple[str, str]:
    return (
        normalize_text(game.get("game_name_kr")).lower(),
        normalize_text(game.get("game_name_en")).lower(),
    )


def make_markdown_game_identity(korean_title: str, english_title: str) -> tuple[str, str]:
    return (normalize_text(korean_title).lower(), normalize_text(english_title).lower())


def has_matching_game_identity(existing_game_keys: set[tuple[str, str]], game: dict[str, str]) -> bool:
    game_name_kr, game_name_en = make_game_identity(game)
    for existing_name_kr, existing_name_en in existing_game_keys:
        if game_name_en and existing_name_en == game_name_en:
            return True
        if game_name_kr and existing_name_kr == game_name_kr:
            return True
    return False


def find_matching_game(games: list[dict[str, str]], candidate: dict[str, str]) -> dict[str, str] | None:
    candidate_name_kr, candidate_name_en = make_game_identity(candidate)
    for game in games:
        game_name_kr, game_name_en = make_game_identity(game)
        if candidate_name_en and game_name_en == candidate_name_en:
            return game
        if candidate_name_kr and game_name_kr == candidate_name_kr:
            return game
    return None


def extract_supported_game_keys_from_markdown(markdown_text: str) -> set[tuple[str, str]]:
    lines = strip_existing_new_games_block(markdown_text).splitlines()
    header_index = None

    for index, line in enumerate(lines):
        if normalize_text(line) == SUPPORTED_GAMES_TABLE_HEADER:
            header_index = index
            break

    if header_index is None:
        return set()

    game_keys: set[tuple[str, str]] = set()
    for line in lines[header_index + 2:]:
        if not normalize_text(line).startswith("|"):
            break
        cells = split_markdown_table_row(line)
        if len(cells) < 2:
            continue
        game_keys.add(make_markdown_game_identity(cells[0], cells[1]))

    return game_keys


def extract_existing_new_games_block(markdown_text: str) -> str:
    lines = str(markdown_text or "").splitlines()
    start_index = None

    for index, line in enumerate(lines):
        if is_new_games_heading(line):
            start_index = index
            break

    if start_index is None:
        return ""

    end_index = len(lines)
    break_markers = {
        WIKI_CAUTION_BLOCKS[0],
        RADEON_IGPU_NOTE_BLOCKS[0],
    }
    seen_new_games_table_header = False

    for index in range(start_index + 1, len(lines)):
        stripped = normalize_text(lines[index])
        if stripped in break_markers:
            end_index = index
            break
        if stripped == NEW_GAMES_TABLE_HEADER:
            if seen_new_games_table_header:
                end_index = index
                break
            seen_new_games_table_header = True
            continue
        if stripped == LEGACY_NEW_GAMES_TABLE_HEADER and not seen_new_games_table_header:
            seen_new_games_table_header = True
            continue

    while end_index > start_index and not normalize_text(lines[end_index - 1]):
        end_index -= 1

    return "\n".join(lines[start_index:end_index])


def strip_existing_new_games_block(markdown_text: str) -> str:
    text = str(markdown_text or "")
    block_text = extract_existing_new_games_block(text)
    if not block_text:
        return text
    return text.replace(block_text, "", 1)


def parse_iso_date(value: Any) -> date | None:
    try:
        return date.fromisoformat(normalize_text(value))
    except ValueError:
        return None


def make_new_game_record(game: dict[str, str], detected_on: str) -> dict[str, str]:
    return {
        "game_name_kr": normalize_text(game.get("game_name_kr")),
        "game_name_en": normalize_text(game.get("game_name_en")),
        "Intel": normalize_text(game.get("Intel")),
        "AMD": normalize_text(game.get("AMD")),
        "NVIDIA": normalize_text(game.get("NVIDIA")),
        "detected_on": normalize_text(detected_on),
    }


def normalize_new_game_record(record: dict[str, str], fallback_detected_on: str) -> dict[str, str] | None:
    normalized = make_new_game_record(record, record.get("detected_on") or fallback_detected_on)
    if not normalized["game_name_kr"] and not normalized["game_name_en"]:
        return None
    if parse_iso_date(normalized["detected_on"]) is None:
        normalized["detected_on"] = fallback_detected_on
    return normalized


def extract_new_games_metadata_records(block_text: str, fallback_detected_on: str) -> list[dict[str, str]]:
    lines = str(block_text or "").splitlines()
    start_index = None

    for index, line in enumerate(lines):
        if normalize_text(line) == NEW_GAMES_METADATA_START:
            start_index = index + 1
            break

    if start_index is None:
        return []

    payload_lines: list[str] = []
    for line in lines[start_index:]:
        if normalize_text(line) == NEW_GAMES_METADATA_END:
            break
        payload_lines.append(line)
    else:
        return []

    try:
        payload = json.loads("\n".join(payload_lines))
    except json.JSONDecodeError:
        return []

    if not isinstance(payload, list):
        return []

    records: list[dict[str, str]] = []
    for item in payload:
        if not isinstance(item, dict):
            continue
        record = normalize_new_game_record(item, fallback_detected_on)
        if record:
            records.append(record)
    return records


def extract_new_games_table_records(block_text: str, fallback_detected_on: str) -> list[dict[str, str]]:
    lines = str(block_text or "").splitlines()
    header_index = None

    for index, line in enumerate(lines):
        if normalize_text(line) in {NEW_GAMES_TABLE_HEADER, LEGACY_NEW_GAMES_TABLE_HEADER}:
            header_index = index
            break

    if header_index is None:
        return []

    records: list[dict[str, str]] = []
    for line in lines[header_index + 2:]:
        if not normalize_text(line).startswith("|"):
            break
        cells = split_markdown_table_row(line)
        if len(cells) < 2:
            continue
        record = normalize_new_game_record(
            {
                "game_name_kr": cells[0],
                "game_name_en": cells[1],
                "Intel": cells[2] if len(cells) > 2 else "",
                "AMD": cells[3] if len(cells) > 3 else "",
                "NVIDIA": cells[4] if len(cells) > 4 else "",
                "detected_on": fallback_detected_on,
            },
            fallback_detected_on,
        )
        if record:
            records.append(record)
    return records


def extract_existing_new_game_records(markdown_text: str, fallback_detected_on: str) -> list[dict[str, str]]:
    block_text = extract_existing_new_games_block(markdown_text)
    if not block_text:
        return []

    records = extract_new_games_metadata_records(block_text, fallback_detected_on)
    if records:
        return records
    return extract_new_games_table_records(block_text, fallback_detected_on)


def should_keep_new_game_record(record: dict[str, str], today: date, retention_days: int) -> bool:
    detected_on = parse_iso_date(record.get("detected_on"))
    if detected_on is None:
        detected_on = today
    cutoff = today - timedelta(days=retention_days)
    return detected_on >= cutoff


def add_new_game_record(records: list[dict[str, str]], record: dict[str, str]) -> None:
    if has_matching_game_identity({make_game_identity(item) for item in records}, record):
        return
    records.append(record)


def sort_new_game_records(records: list[dict[str, str]]) -> list[dict[str, str]]:
    return sorted(
        records,
        key=lambda record: (
            -(parse_iso_date(record.get("detected_on")) or date.min).toordinal(),
            normalize_text(record.get("game_name_kr")).lower(),
            normalize_text(record.get("game_name_en")).lower(),
        ),
    )


def build_new_games_block(new_game_records: list[dict[str, str]]) -> str:
    if not new_game_records:
        return ""

    new_game_records = sort_new_game_records(new_game_records)

    metadata = [
        {
            "game_name_kr": record["game_name_kr"],
            "game_name_en": record["game_name_en"],
            "Intel": record["Intel"],
            "AMD": record["AMD"],
            "NVIDIA": record["NVIDIA"],
            "detected_on": record["detected_on"],
        }
        for record in new_game_records
    ]

    lines = [
        NEW_GAMES_HEADING,
        "",
        NEW_GAMES_METADATA_START,
        json.dumps(metadata, ensure_ascii=False, indent=2),
        NEW_GAMES_METADATA_END,
        "",
        NEW_GAMES_TABLE_HEADER,
        NEW_GAMES_TABLE_SEPARATOR,
    ]

    for game in new_game_records:
        lines.append(
            f"| {escape_md(game['game_name_kr'])} | "
            f"{escape_md(game['game_name_en'])} | "
            f"{escape_md(game['Intel'])} | "
            f"{escape_md(game['AMD'])} | "
            f"{escape_md(game['NVIDIA'])} |"
        )

    return "\n".join(lines)


def build_new_game_support_json_payload(
    new_game_records: list[dict[str, str]],
) -> list[dict[str, str]]:
    payload: list[dict[str, str]] = []

    for record in sort_new_game_records(new_game_records):
        game_name_kr = normalize_text(record.get("game_name_kr"))
        game_name_en = normalize_text(record.get("game_name_en"))
        detected_on = normalize_text(record.get("detected_on"))

        if not game_name_kr and not game_name_en:
            continue

        if parse_iso_date(detected_on) is None:
            continue

        payload.append(
            {
                "game_name_kr": game_name_kr,
                "game_name_en": game_name_en,
                "detected_on": detected_on,
            }
        )

    return payload


def write_new_game_support_json(new_game_records: list[dict[str, str]]) -> None:
    payload = build_new_game_support_json_payload(new_game_records)

    NEW_GAME_SUPPORT_JSON_PATH.parent.mkdir(parents=True, exist_ok=True)
    NEW_GAME_SUPPORT_JSON_PATH.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    print(
        "Updated app new game support data: "
        f"{NEW_GAME_SUPPORT_JSON_PATH.relative_to(ROOT_DIR)} "
        f"({len(payload)} records)"
    )


def refresh_new_game_record_from_current_games(
    record: dict[str, str],
    games: list[dict[str, str]],
) -> dict[str, str]:
    matching_game = find_matching_game(games, record)
    if not matching_game:
        return record
    return make_new_game_record(matching_game, record["detected_on"])


def build_new_game_records_for_outputs(
    games: list[dict[str, str]],
    existing_markdown_text: str,
    *,
    retention_days: int,
) -> list[dict[str, str]]:
    existing_game_keys = extract_supported_game_keys_from_markdown(existing_markdown_text)
    today = date.today()
    today_text = today.isoformat()

    new_game_records = [
        record
        for record in extract_existing_new_game_records(existing_markdown_text, today_text)
        if should_keep_new_game_record(record, today, retention_days)
    ]

    for game in games:
        if has_matching_game_identity(existing_game_keys, game):
            continue
        add_new_game_record(new_game_records, make_new_game_record(game, today_text))

    refreshed_records = [
        refresh_new_game_record_from_current_games(record, games)
        for record in new_game_records
    ]

    return sort_new_game_records(refreshed_records)


def apply_new_games_block_from_records(
    markdown_text: str,
    new_game_records: list[dict[str, str]],
) -> str:
    new_games_block = build_new_games_block(new_game_records)
    markdown_without_existing_new_games = strip_existing_new_games_block(markdown_text).lstrip()

    if not new_games_block:
        return markdown_without_existing_new_games

    return f"{new_games_block}\n\n{markdown_without_existing_new_games}"


def apply_new_games_block(
    markdown_text: str,
    games: list[dict[str, str]],
    existing_markdown_text: str,
    *,
    retention_days: int,
) -> str:
    new_game_records = build_new_game_records_for_outputs(
        games,
        existing_markdown_text,
        retention_days=retention_days,
    )
    return apply_new_games_block_from_records(markdown_text, new_game_records)


def run_git(args: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=cwd,
        text=True,
        check=True,
        capture_output=True,
    )


def run_git_stream(args: list[str], cwd: Path) -> None:
    subprocess.run(["git", *args], cwd=cwd, check=True)


def build_wiki_clone_url() -> str:
    if not WIKI_PUSH_TOKEN:
        return WIKI_REPO_URL
    return WIKI_REPO_URL.replace("https://", f"https://x-access-token:{WIKI_PUSH_TOKEN}@", 1)


def read_text_if_exists(path: Path) -> str:
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8")


def write_if_changed(path: Path, content: str) -> bool:
    existing = read_text_if_exists(path)
    if existing == content:
        return False
    path.write_text(content, encoding="utf-8", newline="\n")
    return True


def configure_git_user(wiki_dir: Path) -> None:
    run_git(["config", "user.name", "github-actions[bot]"], wiki_dir)
    run_git(["config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com"], wiki_dir)


def has_git_changes(wiki_dir: Path) -> bool:
    result = run_git(["status", "--porcelain"], wiki_dir)
    return bool(result.stdout.strip())


def commit_and_push_if_changed(wiki_dir: Path, target_file: str) -> None:
    if not has_git_changes(wiki_dir):
        print("No wiki changes to commit.")
        return

    configure_git_user(wiki_dir)
    run_git_stream(["add", target_file], wiki_dir)
    run_git_stream(["commit", "-m", "Update supported game list"], wiki_dir)
    run_git_stream(["push"], wiki_dir)


def update_wiki_page(markdown_text: str, games: list[dict[str, str]], retention_days: int) -> None:
    if not TARGET_WIKI_PAGE_FILE:
        raise RuntimeError("TARGET_WIKI_PAGE_FILE must not be empty.")
    if not BASELINE_WIKI_PAGE_FILE:
        raise RuntimeError("BASELINE_WIKI_PAGE_FILE must not be empty.")

    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        wiki_dir = temp_path / "wiki"

        clone_url = build_wiki_clone_url()
        run_git_stream(["clone", clone_url, str(wiki_dir)], temp_path)

        target_page_path = wiki_dir / TARGET_WIKI_PAGE_FILE
        baseline_page_path = wiki_dir / BASELINE_WIKI_PAGE_FILE

        existing_markdown_text = read_text_if_exists(baseline_page_path)
        if not existing_markdown_text:
            existing_markdown_text = read_text_if_exists(target_page_path)

        new_game_records = build_new_game_records_for_outputs(
            games,
            existing_markdown_text,
            retention_days=retention_days,
        )
        final_markdown = apply_new_games_block_from_records(markdown_text, new_game_records)
        write_new_game_support_json(new_game_records)

        changed = write_if_changed(target_page_path, final_markdown)
        if not changed:
            print("Supported game list is already up to date.")
        else:
            commit_and_push_if_changed(wiki_dir, TARGET_WIKI_PAGE_FILE)

        return


def main() -> None:
    games = build_games()
    markdown_text = build_markdown(games)
    retention_days = require_int_env_value("NEW_GAMES_RETENTION_DAYS", 15)
    update_wiki_page(markdown_text, games, retention_days)
    print(f"Updated supported game list with {len(games)} games.")


if __name__ == "__main__":
    main()

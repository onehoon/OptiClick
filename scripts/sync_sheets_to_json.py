import json
import os
import re
from pathlib import Path
from typing import Any, Dict, List

from google.oauth2 import service_account
from googleapiclient.discovery import build


SCOPES = ["https://www.googleapis.com/auth/spreadsheets.readonly"]


def normalize_text(value: Any) -> str:
    if value is None:
        return ""
    return str(value).strip()


def normalize_header(value: Any) -> str:
    return re.sub(r"\s+", "_", normalize_text(value).lower())


def is_cell_empty(value: Any) -> bool:
    return value is None or str(value).strip() == ""


def is_row_completely_empty(row: List[Any]) -> bool:
    return all(is_cell_empty(cell) for cell in row)


def find_duplicate_headers(headers: List[str]) -> List[str]:
    seen = set()
    duplicates = set()

    for header in headers:
        if not header:
            continue
        if header in seen:
            duplicates.add(header)
        seen.add(header)

    return sorted(duplicates)


def escape_sheet_name_for_a1(sheet_name: str) -> str:
    escaped = sheet_name.replace("'", "''")
    return f"'{escaped}'"


def pad_row(row: List[Any], length: int) -> List[Any]:
    if len(row) >= length:
        return row[:length]
    return row + [""] * (length - len(row))


def build_json_rows_from_values(values: List[List[Any]], sheet_name: str) -> List[Dict[str, Any]]:
    if not values:
        raise ValueError(f"{sheet_name}: 빈 시트")

    headers = [normalize_header(value) for value in values[0]]
    normalized_headers = [header for header in headers if header]

    if not normalized_headers:
        raise ValueError(f"{sheet_name}: 헤더 없음")

    duplicates = find_duplicate_headers(normalized_headers)
    if duplicates:
        raise ValueError(f"{sheet_name}: 헤더 중복: {', '.join(duplicates)}")

    profile_id_idx = headers.index("profile_id") if "profile_id" in headers else -1
    header_length = len(headers)
    output: List[Dict[str, Any]] = []

    for raw_row in values[1:]:
        row = pad_row(raw_row, header_length)

        if is_row_completely_empty(row):
            continue

        if profile_id_idx > -1:
            profile_id = normalize_text(row[profile_id_idx])
            if profile_id.startswith("#"):
                continue

        item: Dict[str, Any] = {}
        has_any_data = False

        for index, key in enumerate(headers):
            if not key:
                continue

            value = row[index]
            item[key] = value

            if not is_cell_empty(value):
                has_any_data = True

        if not has_any_data:
            continue

        output.append(item)

    return output


def get_sheet_values(service: Any, spreadsheet_id: str, sheet_names: List[str]) -> Dict[str, List[List[Any]]]:
    ranges = [escape_sheet_name_for_a1(sheet_name) for sheet_name in sheet_names]

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
    result: Dict[str, List[List[Any]]] = {}

    for sheet_name, value_range in zip(sheet_names, value_ranges):
        result[sheet_name] = value_range.get("values", [])

    return result


def parse_sheet_names() -> List[str]:
    input_sheet_names = os.environ.get("INPUT_SHEET_NAMES", "").strip()
    default_sheet_names = os.environ.get("DEFAULT_SHEET_NAMES", "").strip()

    raw = input_sheet_names or default_sheet_names
    sheet_names = [name.strip() for name in raw.split(",") if name.strip()]

    if not sheet_names:
        raise ValueError("동기화할 시트명이 없습니다.")

    return sheet_names


def main() -> None:
    credentials_path = os.environ["GOOGLE_APPLICATION_CREDENTIALS"]
    spreadsheet_id = os.environ["GOOGLE_SPREADSHEET_ID"]
    output_dir = Path(os.environ.get("OUTPUT_DIR", "assets/data"))

    sheet_names = parse_sheet_names()

    credentials = service_account.Credentials.from_service_account_file(
        credentials_path,
        scopes=SCOPES,
    )

    service = build("sheets", "v4", credentials=credentials, cache_discovery=False)

    output_dir.mkdir(parents=True, exist_ok=True)

    values_by_sheet = get_sheet_values(service, spreadsheet_id, sheet_names)

    for sheet_name in sheet_names:
        values = values_by_sheet.get(sheet_name, [])
        rows = build_json_rows_from_values(values, sheet_name)

        output_path = output_dir / f"{sheet_name}.json"

        with output_path.open("w", encoding="utf-8", newline="\n") as file:
            json.dump(rows, file, ensure_ascii=False, indent=2)
            file.write("\n")

        print(f"[OK] {sheet_name} -> {output_path} ({len(rows)} rows)")


if __name__ == "__main__":
    main()

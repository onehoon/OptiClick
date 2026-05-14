from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


VALID_VENDORS = {"amd", "nvidia", "intel"}


def _load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def _assert(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def validate_manifest(manifest_path: Path) -> dict[str, Any]:
    _assert(manifest_path.exists(), f"manifest file not found: {manifest_path}")
    payload = _load_json(manifest_path)
    _assert(isinstance(payload, dict), "manifest must be a JSON object")
    rules = payload.get("rules")
    _assert(isinstance(rules, list), "manifest.rules must be a list")
    _assert(len(rules) > 0, "manifest.rules must not be empty")

    for index, rule in enumerate(rules, start=1):
        _assert(isinstance(rule, dict), f"manifest.rules[{index}] must be an object")
        vendor = str(rule.get("vendor", "")).strip().lower()
        bundle_key = str(rule.get("bundle_key", "")).strip().lower()
        gpu_group = str(rule.get("gpu_group", "")).strip().lower()
        _assert(vendor in VALID_VENDORS, f"manifest.rules[{index}].vendor is invalid: {vendor}")
        _assert(bundle_key != "", f"manifest.rules[{index}].bundle_key is empty")
        _assert(gpu_group != "", f"manifest.rules[{index}].gpu_group is empty")
    return payload


def validate_bundle_file(path: Path) -> None:
    payload = _load_json(path)
    _assert(isinstance(payload, dict), f"{path} must be a JSON object")
    _assert(payload.get("ok") is True, f"{path} must contain ok=true")

    games = payload.get("games")
    _assert(isinstance(games, list), f"{path} games must be a list")
    _assert(len(games) > 0, f"{path} games must not be empty")

    profiles = payload.get("profiles")
    _assert(isinstance(profiles, dict), f"{path} profiles must be an object")
    optiscaler_ini = profiles.get("optiscaler_ini")
    _assert(isinstance(optiscaler_ini, list), f"{path} profiles.optiscaler_ini must be a list")

    for game_index, game in enumerate(games, start=1):
        _assert(isinstance(game, dict), f"{path} games[{game_index}] must be an object")
        _assert(str(game.get("game_id", "")).strip() != "", f"{path} games[{game_index}].game_id is empty")
        _assert(str(game.get("profile_id", "")).strip() != "", f"{path} games[{game_index}].profile_id is empty")
        install_profile = game.get("install_profile")
        _assert(
            isinstance(install_profile, dict),
            f"{path} games[{game_index}].install_profile must be an object",
        )
        _assert(
            install_profile.get("enabled") is True,
            f"{path} games[{game_index}].install_profile.enabled must be true",
        )

    for row_index, ini_row in enumerate(optiscaler_ini, start=1):
        _assert(isinstance(ini_row, dict), f"{path} profiles.optiscaler_ini[{row_index}] must be an object")
        if "enabled" in ini_row:
            _assert(ini_row.get("enabled") is True, f"{path} ini row {row_index} has enabled != true")


def validate_bundles(manifest_payload: dict[str, Any], bundles_dir: Path) -> None:
    _assert(bundles_dir.exists(), f"bundle dir not found: {bundles_dir}")
    rules = manifest_payload["rules"]
    bundle_keys = {(str(rule["vendor"]).lower(), str(rule["bundle_key"]).lower()) for rule in rules}
    for vendor, bundle_key in sorted(bundle_keys):
        bundle_path = bundles_dir / vendor / f"{bundle_key}.json"
        _assert(bundle_path.exists(), f"bundle not found for {vendor}/{bundle_key}: {bundle_path}")
        validate_bundle_file(bundle_path)


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate generated GPU bundle JSON files.")
    parser.add_argument(
        "--manifest",
        default="dist/gpu-bundles/manifest.json",
        help="Path to generated manifest.json",
    )
    parser.add_argument(
        "--bundles-dir",
        default="dist/gpu-bundles",
        help="Directory that contains vendor bundle json files",
    )
    args = parser.parse_args()

    manifest_path = Path(args.manifest)
    bundles_dir = Path(args.bundles_dir)

    manifest_payload = validate_manifest(manifest_path)
    validate_bundles(manifest_payload, bundles_dir)
    print("[OK] GPU bundle validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

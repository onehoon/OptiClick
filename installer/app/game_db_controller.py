from __future__ import annotations

from collections.abc import Callable
from concurrent.futures import Executor
from dataclasses import dataclass, field
import logging
from typing import Any, Protocol

from ..common.log_sanitizer import redact_text
from ..data import gpu_bundle_loader, message_loader, new_game_support_loader, profile_loader, runtime_data_loader


SchedulerCallback = Callable[[Callable[[], None]], Any]
GameDbLoader = Callable[[object], dict[str, dict[str, Any]]]
ModuleLinksLoader = Callable[[object], dict[str, Any]]
RuntimeDataLoader = Callable[[], dict[str, Any]]
MessageCenterRowsLoader = Callable[[object], dict[str, message_loader.MessageTemplate]]
MessageBindingRowsLoader = Callable[[object], tuple[message_loader.MessageBinding, ...]]
MessageRepoBuilder = Callable[[dict[str, message_loader.MessageTemplate], tuple[message_loader.MessageBinding, ...]], message_loader.MessageRepository]
GpuBundleMerger = Callable[[dict[str, dict[str, Any]], dict[str, dict[str, Any]]], dict[str, dict[str, Any]]]
ProfileCatalogRowsLoader = Callable[..., profile_loader.ProfileCatalogs]
ProfileCatalogAttacher = Callable[[dict[str, dict[str, Any]], profile_loader.ProfileCatalogs], dict[str, dict[str, Any]]]
NewGameSupportLoader = Callable[[str], tuple[new_game_support_loader.NewGameSupportEntry, ...]]


class MessageMaterializer(Protocol):
    def __call__(
        self,
        game_db: dict[str, dict[str, Any]],
        repository: message_loader.MessageRepository,
        *,
        gpu_vendor: str = "",
    ) -> dict[str, dict[str, Any]]:
        ...


class GpuBundleLoader(Protocol):
    def __call__(
        self,
        base_url_or_key: str,
        gpu_vendor: str,
        gpu_model: str,
    ) -> dict[str, dict[str, Any]]:
        ...


class NewGameSupportPopupBuilder(Protocol):
    def __call__(
        self,
        entries: tuple[new_game_support_loader.NewGameSupportEntry, ...] | list[new_game_support_loader.NewGameSupportEntry],
        *,
        lang: str,
    ) -> str:
        ...


@dataclass(frozen=True)
class GameDbLoadResult:
    game_db: dict[str, dict[str, Any]]
    ok: bool
    error: Exception | None
    module_download_links: dict[str, Any] = field(default_factory=dict)
    game_db_vendor: str = "default"


@dataclass(frozen=True)
class GameDbControllerCallbacks:
    on_load_complete: Callable[[GameDbLoadResult], None]


class GameDbLoadController:
    def __init__(
        self,
        *,
        executor: Executor,
        schedule: SchedulerCallback,
        callbacks: GameDbControllerCallbacks,
        load_game_db: GameDbLoader,
        load_module_download_links: ModuleLinksLoader,
        load_runtime_data: RuntimeDataLoader = runtime_data_loader.load_runtime_data,
        parse_message_center_rows: MessageCenterRowsLoader = message_loader.parse_message_center_rows,
        parse_message_binding_rows: MessageBindingRowsLoader = message_loader.parse_message_binding_rows,
        build_message_repository: MessageRepoBuilder = message_loader.build_message_repository,
        materialize_bound_messages: MessageMaterializer = message_loader.materialize_bound_messages_into_game_db,
        gpu_bundle_url: str = "",
        load_gpu_bundle: GpuBundleLoader = gpu_bundle_loader.load_supported_game_bundle,
        merge_gpu_bundle: GpuBundleMerger = gpu_bundle_loader.merge_gpu_bundle_into_game_db,
        build_profile_catalogs_from_rows: ProfileCatalogRowsLoader = profile_loader.build_profile_catalogs_from_rows,
        new_game_support_url: str = "",
        load_new_game_support: NewGameSupportLoader = new_game_support_loader.load_new_game_support,
        build_new_game_support_popup_text: NewGameSupportPopupBuilder = new_game_support_loader.build_new_game_support_popup_text,
        attach_profile_catalogs: ProfileCatalogAttacher = profile_loader.attach_profile_catalogs_to_game_db,
        logger=None,
    ) -> None:
        self._executor = executor
        self._schedule = schedule
        self._callbacks = callbacks
        self._load_game_db = load_game_db
        self._load_module_download_links = load_module_download_links
        self._load_runtime_data = load_runtime_data
        self._parse_message_center_rows = parse_message_center_rows
        self._parse_message_binding_rows = parse_message_binding_rows
        self._build_message_repository = build_message_repository
        self._materialize_bound_messages = materialize_bound_messages
        self._gpu_bundle_url = str(gpu_bundle_url or "").strip()
        self._load_gpu_bundle = load_gpu_bundle
        self._merge_gpu_bundle = merge_gpu_bundle
        self._build_profile_catalogs_from_rows = build_profile_catalogs_from_rows
        self._new_game_support_url = str(new_game_support_url or "").strip()
        self._load_new_game_support = load_new_game_support
        self._build_new_game_support_popup_text = build_new_game_support_popup_text
        self._attach_profile_catalogs = attach_profile_catalogs
        self._logger = logger or logging.getLogger()

        self._load_started = False

    def start_load(self, game_db_vendor: str, gpu_model: str = "") -> bool:
        if self._load_started:
            return False

        self._load_started = True
        normalized_vendor = str(game_db_vendor or "default")
        normalized_gpu_model = str(gpu_model or "").strip()

        try:
            self._executor.submit(self._run_load_worker, normalized_vendor, normalized_gpu_model)
        except Exception as exc:
            self._logger.exception("Failed to submit game DB load worker")
            self._schedule_result(
                GameDbLoadResult(
                    game_db={},
                    module_download_links={},
                    ok=False,
                    error=exc,
                    game_db_vendor=normalized_vendor,
                ),
                description="game DB load failure callback",
            )
            return False

        return True

    def _run_load_worker(self, game_db_vendor: str, gpu_model: str = "") -> None:
        try:
            runtime_data = self._load_runtime_data()
            game_db = self._load_base_game_db(runtime_data)
            message_repo = self._load_message_repository(runtime_data)
            game_db = self._materialize_messages(game_db, message_repo, game_db_vendor=game_db_vendor)
            game_db = self._merge_gpu_bundle_if_configured(
                game_db,
                game_db_vendor=game_db_vendor,
                gpu_model=gpu_model,
            )
            game_db = self._attach_profile_catalogs_if_configured(game_db, runtime_data)
            module_links = self._load_module_links(runtime_data)
            self._inject_startup_warning_links(
                module_links,
                message_repo,
                game_db_vendor=game_db_vendor,
            )
            self._inject_new_game_support_links(module_links)
            result = self._build_success_result(
                game_db,
                module_links,
                game_db_vendor=game_db_vendor,
            )
        except Exception as exc:
            if isinstance(exc, runtime_data_loader.RuntimeDataError):
                cloudflare_status = runtime_data_loader.check_cloudflare_status()
                self._logger.error(
                    "runtime-data load failed: %s; cloudflare_status=%s; cloudflare_status_description=%s",
                    redact_text(exc),
                    cloudflare_status.get("indicator", "unknown"),
                    cloudflare_status.get("description", ""),
                )
            result = self._build_failure_result(exc, game_db_vendor=game_db_vendor)

        self._schedule_result(result, description="game DB load completion callback")

    def _load_base_game_db(self, runtime_data: dict[str, Any]) -> dict[str, dict[str, Any]]:
        game_db = self._load_game_db(runtime_data.get("game_master", []))
        if not game_db:
            raise ValueError("Game DB has no data.")
        return game_db

    def _load_message_repository(self, runtime_data: dict[str, Any]) -> message_loader.MessageRepository:
        message_center = self._parse_message_center_rows(runtime_data.get("message_center", []))
        message_binding = self._parse_message_binding_rows(runtime_data.get("message_binding", []))
        return self._build_message_repository(message_center, message_binding)

    def _materialize_messages(
        self,
        game_db: dict[str, dict[str, Any]],
        message_repo: message_loader.MessageRepository,
        *,
        game_db_vendor: str,
    ) -> dict[str, dict[str, Any]]:
        return self._materialize_bound_messages(
            game_db,
            message_repo,
            gpu_vendor=game_db_vendor,
        )

    def _merge_gpu_bundle_if_configured(
        self,
        game_db: dict[str, dict[str, Any]],
        *,
        game_db_vendor: str,
        gpu_model: str,
    ) -> dict[str, dict[str, Any]]:
        if not self._should_load_gpu_bundle(game_db_vendor):
            return game_db

        # GPU bundle is vendor-specific runtime data; if configured, a failed fetch must fail closed.
        try:
            bundle = self._load_gpu_bundle(self._gpu_bundle_url, game_db_vendor, gpu_model)
            return self._merge_gpu_bundle(game_db, bundle)
        except Exception as bundle_err:
            self._logger.error("Failed to load GPU bundle: %s", redact_text(bundle_err))
            raise RuntimeError("GPU bundle load failed") from bundle_err

    def _should_load_gpu_bundle(self, game_db_vendor: str) -> bool:
        return bool(self._gpu_bundle_url and game_db_vendor and game_db_vendor != "default")

    def _attach_profile_catalogs_if_configured(
        self,
        game_db: dict[str, dict[str, Any]],
        runtime_data: dict[str, Any],
    ) -> dict[str, dict[str, Any]]:
        try:
            catalogs = self._build_profile_catalogs_from_rows(
                game_ini_profile_rows=runtime_data.get("game_ini_profile", []),
                game_unreal_ini_profile_rows=runtime_data.get("game_unreal_ini_profile", []),
                engine_ini_profile_rows=runtime_data.get("engine_ini_profile", []),
                game_xml_profile_rows=runtime_data.get("game_xml_profile", []),
                registry_profile_rows=runtime_data.get("registry_profile", []),
                game_json_profile_rows=runtime_data.get("game_json_profile", []),
            )
            return self._attach_profile_catalogs(game_db, catalogs)
        except Exception as profile_err:
            self._logger.error("Failed to load profile catalogs: %s", redact_text(profile_err))
            raise RuntimeError("Profile catalog load failed") from profile_err

    def _load_module_links(self, runtime_data: dict[str, Any]) -> dict[str, Any]:
        return self._load_module_download_links(runtime_data.get("resource_master", []))

    def _inject_startup_warning_links(
        self,
        module_links: dict[str, Any],
        message_repo: message_loader.MessageRepository,
        *,
        game_db_vendor: str,
    ) -> None:
        warning_ko = self._resolve_startup_warning_text(
            message_repo,
            game_db_vendor=game_db_vendor,
            lang="ko",
        )
        warning_en = self._resolve_startup_warning_text(
            message_repo,
            game_db_vendor=game_db_vendor,
            lang="en",
        )
        if warning_ko:
            module_links["__warning_kr__"] = warning_ko
        if warning_en:
            module_links["__warning_en__"] = warning_en

    def _resolve_startup_warning_text(
        self,
        message_repo: message_loader.MessageRepository,
        *,
        game_db_vendor: str,
        lang: str,
    ) -> str:
        return message_loader.resolve_startup_warning_text(
            message_repo,
            gpu_vendor=game_db_vendor,
            lang=lang,
        )

    def _inject_new_game_support_links(self, module_links: dict[str, Any]) -> None:
        if not self._new_game_support_url:
            return

        try:
            entries = self._load_new_game_support(self._new_game_support_url)
        except Exception as exc:
            self._logger.info("Failed to load new game support data: %s", redact_text(exc))
            return

        try:
            text_ko = self._build_new_game_support_popup_text(entries, lang="ko")
            text_en = self._build_new_game_support_popup_text(entries, lang="en")
        except Exception as exc:
            self._logger.info("Failed to build new game support popup text: %s", redact_text(exc))
            return

        if text_ko:
            module_links["__new_game_support_kr__"] = text_ko
        if text_en:
            module_links["__new_game_support_en__"] = text_en

    def _build_success_result(
        self,
        game_db: dict[str, dict[str, Any]],
        module_download_links: dict[str, Any],
        *,
        game_db_vendor: str,
    ) -> GameDbLoadResult:
        return GameDbLoadResult(
            game_db=game_db,
            module_download_links=module_download_links,
            ok=True,
            error=None,
            game_db_vendor=game_db_vendor,
        )

    def _build_failure_result(
        self,
        error: Exception,
        *,
        game_db_vendor: str,
    ) -> GameDbLoadResult:
        return GameDbLoadResult(
            game_db={},
            module_download_links={},
            ok=False,
            error=error,
            game_db_vendor=game_db_vendor,
        )

    def _schedule_result(self, result: GameDbLoadResult, *, description: str) -> None:
        try:
            self._schedule(lambda load_result=result: self._callbacks.on_load_complete(load_result))
        except Exception:
            self._logger.exception("Failed to schedule %s", description)

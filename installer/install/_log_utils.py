from __future__ import annotations


class _SuppressInfoLogger:
    def __init__(self, logger) -> None:
        self._logger = logger

    def info(self, *args, **kwargs) -> None:
        return None

    def __getattr__(self, name: str):
        return getattr(self._logger, name)

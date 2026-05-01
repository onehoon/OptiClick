import threading
import time
from urllib.parse import parse_qsl, urlencode, urlparse, urlunparse

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry


_SHARED_RETRY_SESSION = None
_SHARED_RETRY_SESSION_LOCK = threading.Lock()
_GITHUB_RAW_HOST = "raw.githubusercontent.com"
_DATA_CACHE_BUST_QUERY_KEY = "ts"
_DATA_CACHE_BUST_TOKEN = str(int(time.time()))


def build_retry_session(total=3, backoff_factor=1, status_forcelist=(429, 500, 502, 503, 504), allowed_methods=("GET", "HEAD")):
    session = requests.Session()
    retry_strategy = Retry(
        total=total,
        backoff_factor=backoff_factor,
        status_forcelist=status_forcelist,
        allowed_methods=allowed_methods,
    )
    adapter = HTTPAdapter(max_retries=retry_strategy)
    session.mount("https://", adapter)
    session.mount("http://", adapter)
    return session


def get_shared_retry_session():
    global _SHARED_RETRY_SESSION

    if _SHARED_RETRY_SESSION is None:
        with _SHARED_RETRY_SESSION_LOCK:
            if _SHARED_RETRY_SESSION is None:
                _SHARED_RETRY_SESSION = build_retry_session()
    return _SHARED_RETRY_SESSION


def add_github_raw_data_cache_bust(url: str) -> str:
    normalized = str(url or "").strip()
    if not normalized:
        return normalized

    parsed = urlparse(normalized)
    path = str(parsed.path or "").replace("\\", "/")
    if (
        parsed.scheme.lower() not in {"http", "https"}
        or parsed.netloc.casefold() != _GITHUB_RAW_HOST
        or "/assets/data/" not in path
        or not path.casefold().endswith(".json")
    ):
        return normalized

    query = [
        (key, value)
        for key, value in parse_qsl(parsed.query, keep_blank_values=True)
        if key.casefold() != _DATA_CACHE_BUST_QUERY_KEY
    ]
    query.append((_DATA_CACHE_BUST_QUERY_KEY, _DATA_CACHE_BUST_TOKEN))
    return urlunparse(parsed._replace(query=urlencode(query)))

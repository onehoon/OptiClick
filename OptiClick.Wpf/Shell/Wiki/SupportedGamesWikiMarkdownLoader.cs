using System.Net;
using System.Net.Http;
using System.Globalization;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Wiki;

public sealed class SupportedGamesWikiMarkdownRefreshResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public bool DidUpdate { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyList<SupportedGamesWikiEntry> Entries { get; init; } = [];

    public static SupportedGamesWikiMarkdownRefreshResult Success(
        IReadOnlyList<SupportedGamesWikiEntry> entries,
        bool didUpdate = true)
    {
        return new SupportedGamesWikiMarkdownRefreshResult
        {
            DidRun = true,
            IsSuccess = true,
            DidUpdate = didUpdate,
            Entries = entries ?? []
        };
    }

    public static SupportedGamesWikiMarkdownRefreshResult NotModified()
    {
        return new SupportedGamesWikiMarkdownRefreshResult
        {
            DidRun = true,
            IsSuccess = true,
            DidUpdate = false,
            ErrorCode = "not_modified"
        };
    }

    public static SupportedGamesWikiMarkdownRefreshResult Skipped(string errorCode = "")
    {
        return new SupportedGamesWikiMarkdownRefreshResult
        {
            DidRun = false,
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    public static SupportedGamesWikiMarkdownRefreshResult Failure(string errorCode)
    {
        return new SupportedGamesWikiMarkdownRefreshResult
        {
            DidRun = true,
            IsSuccess = false,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "request_failed" : errorCode.Trim()
        };
    }
}

public interface ISupportedGamesWikiMarkdownLoader
{
    IReadOnlyList<SupportedGamesWikiEntry> LoadCachedOrEmpty();
    Task<SupportedGamesWikiMarkdownRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
}

public sealed class NoopSupportedGamesWikiMarkdownLoader : ISupportedGamesWikiMarkdownLoader
{
    public IReadOnlyList<SupportedGamesWikiEntry> LoadCachedOrEmpty() => [];

    public Task<SupportedGamesWikiMarkdownRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(SupportedGamesWikiMarkdownRefreshResult.Skipped("disabled"));
    }
}

public sealed class SupportedGamesWikiMarkdownLoader : ISupportedGamesWikiMarkdownLoader
{
    private readonly SupportedGamesWikiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ISupportedGamesWikiMarkdownParser _parser;
    private readonly ISupportedGamesWikiMarkdownCacheStore _cacheStore;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _retryDelay;
    private readonly int _maxAttempts;

    public SupportedGamesWikiMarkdownLoader(
        SupportedGamesWikiOptions options,
        HttpClient httpClient,
        ISupportedGamesWikiMarkdownParser parser,
        ISupportedGamesWikiMarkdownCacheStore cacheStore,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        TimeSpan? retryDelay = null,
        int maxAttempts = 2)
    {
        _options = options ?? new SupportedGamesWikiOptions();
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(250);
        _maxAttempts = Math.Max(1, maxAttempts);
    }

    public IReadOnlyList<SupportedGamesWikiEntry> LoadCachedOrEmpty()
    {
        var cachedEntriesDocument = _cacheStore.TryReadEntriesDocument();
        if (cachedEntriesDocument?.Entries.Count > 0)
        {
            _logger.Info(
                "wiki-games",
                $"supported_games_view_load completed source=entries_cache entries={cachedEntriesDocument.Entries.Count}");
            return cachedEntriesDocument.Entries;
        }

        var cachedContent = _cacheStore.TryReadContent();
        if (string.IsNullOrWhiteSpace(cachedContent))
        {
            return [];
        }

        var parseResult = _parser.Parse(cachedContent);
        if (!parseResult.IsSuccess)
        {
            var code = NormalizeStatusCode(parseResult.ErrorCode, "invalid_cache_payload");
            _logger.Warning("wiki-games", $"supported games wiki markdown cache parse failed code={code}");
            return [];
        }

        var entries = parseResult.Entries ?? [];
        var metadata = _cacheStore.TryReadMetadata();
        WriteEntriesCache(
            entries,
            metadata?.LastModified ?? "",
            metadata?.ETag ?? "");
        _logger.Info(
            "wiki-games",
            $"supported_games_view_load completed source=markdown_cache entries={entries.Count}");
        return entries;
    }

    public async Task<SupportedGamesWikiMarkdownRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return SupportedGamesWikiMarkdownRefreshResult.Skipped("disabled");
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            return SupportedGamesWikiMarkdownRefreshResult.Skipped("endpoint_missing");
        }

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpointUri))
        {
            return SupportedGamesWikiMarkdownRefreshResult.Failure("invalid_endpoint");
        }

        _logger.Info("wiki-games", "supported_games_cache_check started");
        var localMetadata = _cacheStore.TryReadMetadata();
        var localETag = (localMetadata?.ETag ?? "").Trim();
        var localLastModified = (localMetadata?.LastModified ?? "").Trim();
        var localSourceUrl = (localMetadata?.SourceUrl ?? "").Trim();
        var currentSourceUrl = (_options.Endpoint ?? "").Trim();
        var headResult = await FetchCacheValidatorsAsync(endpointUri, cancellationToken);
        if (!headResult.IsSuccess)
        {
            var code = NormalizeStatusCode(headResult.ErrorCode, "head_failed");
            _logger.Warning("wiki-games", $"supported_games_cache_refresh failed reason=head_failed code={code}");
            return _cacheStore.HasReadableCache()
                ? SupportedGamesWikiMarkdownRefreshResult.Skipped("head_failed")
                : SupportedGamesWikiMarkdownRefreshResult.Failure("head_failed");
        }

        var remoteETag = (headResult.ETag ?? "").Trim();
        var remoteLastModified = (headResult.LastModified ?? "").Trim();
        if (string.IsNullOrWhiteSpace(remoteETag)
            && string.IsNullOrWhiteSpace(remoteLastModified)
            && _cacheStore.HasReadableCache())
        {
            _logger.Info("wiki-games", "supported_games_cache_check skipped reason=remote_validator_missing");
            return SupportedGamesWikiMarkdownRefreshResult.Skipped("remote_validator_missing");
        }

        if (IsRemoteCacheUnchanged(localSourceUrl, currentSourceUrl, localETag, remoteETag, localLastModified, remoteLastModified)
            && _cacheStore.HasEntriesCache())
        {
            _logger.Info("wiki-games", "supported_games_cache_check skipped reason=not_modified");
            return SupportedGamesWikiMarkdownRefreshResult.NotModified();
        }

        _logger.Info("wiki-games", "supported_games_cache_refresh started");
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                var fetchResult = await FetchContentAsync(endpointUri, timeoutCts.Token);
                if (!fetchResult.IsSuccess)
                {
                    if (ShouldRetry(fetchResult.ErrorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, fetchResult.ErrorCode, cancellationToken))
                    {
                        continue;
                    }

                    return SupportedGamesWikiMarkdownRefreshResult.Failure(fetchResult.ErrorCode);
                }

                var parseResult = _parser.Parse(fetchResult.Content);
                if (!parseResult.IsSuccess)
                {
                    return SupportedGamesWikiMarkdownRefreshResult.Failure(
                        NormalizeStatusCode(parseResult.ErrorCode, "invalid_remote_payload"));
                }

                _cacheStore.TryWriteContent(fetchResult.Content);
                var entries = parseResult.Entries ?? [];
                var effectiveETag = string.IsNullOrWhiteSpace(fetchResult.ETag)
                    ? remoteETag
                    : fetchResult.ETag;
                var effectiveLastModified = string.IsNullOrWhiteSpace(fetchResult.LastModified)
                    ? remoteLastModified
                    : fetchResult.LastModified;
                WriteEntriesCache(entries, effectiveLastModified, effectiveETag);
                WriteMetadata(effectiveLastModified, effectiveETag);
                _logger.Info("wiki-games", $"supported_games_cache_refresh completed entries={entries.Count}");
                return SupportedGamesWikiMarkdownRefreshResult.Success(entries);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return SupportedGamesWikiMarkdownRefreshResult.Failure("canceled");
            }
            catch (OperationCanceledException)
            {
                const string errorCode = "timeout";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                return SupportedGamesWikiMarkdownRefreshResult.Failure(errorCode);
            }
            catch (HttpRequestException)
            {
                const string errorCode = "request_failed";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                return SupportedGamesWikiMarkdownRefreshResult.Failure(errorCode);
            }
            catch
            {
                const string errorCode = "unexpected_error";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                return SupportedGamesWikiMarkdownRefreshResult.Failure(errorCode);
            }
        }

        return SupportedGamesWikiMarkdownRefreshResult.Failure("request_failed");
    }

    private async Task<(bool IsSuccess, string ErrorCode, string LastModified, string ETag)> FetchCacheValidatorsAsync(
        Uri endpointUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, endpointUri);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"http_{(int)response.StatusCode}", "", "");
            }

            return (true, "", ReadLastModified(response), ReadETag(response));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (false, "canceled", "", "");
        }
        catch (OperationCanceledException)
        {
            return (false, "timeout", "", "");
        }
        catch (HttpRequestException)
        {
            return (false, "request_failed", "", "");
        }
        catch
        {
            return (false, "unexpected_error", "", "");
        }
    }

    private async Task<(bool IsSuccess, string ErrorCode, string Content, string LastModified, string ETag)> FetchContentAsync(
        Uri endpointUri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpointUri);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (false, $"http_{(int)response.StatusCode}", "", "", "");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "empty_response", "", "", "");
        }

        return (true, "", content, ReadLastModified(response), ReadETag(response));
    }

    private void WriteEntriesCache(
        IReadOnlyList<SupportedGamesWikiEntry> entries,
        string sourceLastModified,
        string sourceETag)
    {
        if (entries.Count == 0)
        {
            return;
        }

        _cacheStore.TryWriteEntriesDocument(new SupportedGamesWikiEntriesCacheDocument
        {
            Version = 1,
            SourceUrl = (_options.Endpoint ?? "").Trim(),
            SourceETag = (sourceETag ?? "").Trim(),
            SourceLastModified = (sourceLastModified ?? "").Trim(),
            GeneratedAt = UtcNowText(),
            Entries = entries
        });
    }

    private void WriteMetadata(string lastModified, string eTag)
    {
        _cacheStore.TryWriteMetadata(new SupportedGamesWikiCacheMetadata
        {
            SourceUrl = (_options.Endpoint ?? "").Trim(),
            ETag = (eTag ?? "").Trim(),
            LastModified = (lastModified ?? "").Trim(),
            CachedAt = UtcNowText()
        });
    }

    private static string ReadLastModified(HttpResponseMessage response)
    {
        if (response.Content?.Headers.LastModified is { } contentLastModified)
        {
            return contentLastModified.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
        }

        if (response.Headers.TryGetValues("Last-Modified", out var values))
        {
            return values.FirstOrDefault()?.Trim() ?? "";
        }

        return "";
    }

    private static string ReadETag(HttpResponseMessage response)
    {
        if (response.Headers.ETag is { } eTag)
        {
            return eTag.ToString().Trim();
        }

        if (response.Headers.TryGetValues("ETag", out var values))
        {
            return values.FirstOrDefault()?.Trim() ?? "";
        }

        return "";
    }

    private static bool IsRemoteCacheUnchanged(
        string localSourceUrl,
        string currentSourceUrl,
        string localETag,
        string remoteETag,
        string localLastModified,
        string remoteLastModified)
    {
        if (!string.Equals(localSourceUrl, currentSourceUrl, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(remoteETag))
        {
            return string.Equals(localETag, remoteETag, StringComparison.Ordinal);
        }

        return !string.IsNullOrWhiteSpace(remoteLastModified)
               && string.Equals(localLastModified, remoteLastModified, StringComparison.Ordinal);
    }

    private static string UtcNowText()
    {
        return DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    }

    private bool ShouldRetry(string errorCode, int attempt)
    {
        if (attempt >= _maxAttempts)
        {
            return false;
        }

        if (TryParseHttpStatusCode(errorCode, out var statusCode))
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        return string.Equals(errorCode, "timeout", StringComparison.Ordinal)
               || string.Equals(errorCode, "request_failed", StringComparison.Ordinal)
               || string.Equals(errorCode, "empty_response", StringComparison.Ordinal)
               || string.Equals(errorCode, "unexpected_error", StringComparison.Ordinal);
    }

    private static bool TryParseHttpStatusCode(string errorCode, out int statusCode)
    {
        statusCode = 0;
        if (string.IsNullOrWhiteSpace(errorCode)
            || !errorCode.StartsWith("http_", StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(errorCode["http_".Length..], out statusCode);
    }

    private async Task<bool> TryDelayBeforeRetryAsync(
        int attempt,
        string errorCode,
        CancellationToken cancellationToken)
    {
        var nextAttempt = attempt + 1;
        _logger.Warning(
            "wiki-games",
            $"supported games wiki markdown refresh retry scheduled attempt={nextAttempt} code={NormalizeStatusCode(errorCode, "unknown")}");

        if (_retryDelay <= TimeSpan.Zero)
        {
            return true;
        }

        try
        {
            var delay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * attempt);
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

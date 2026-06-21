using System.Net.Http;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Remote;

public sealed class RemoteJsonFetchResult
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Content { get; init; } = "";
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; } = EmptyHeaders;

    public static RemoteJsonFetchResult Success(
        string content,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
    {
        return new RemoteJsonFetchResult
        {
            IsSuccess = true,
            Content = content ?? "",
            Headers = headers ?? EmptyHeaders
        };
    }

    public static RemoteJsonFetchResult Failure(string errorCode)
    {
        return new RemoteJsonFetchResult
        {
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public sealed record RemoteJsonFetchOptions
{
    public string LogCategory { get; init; } = "Remote";
    public string RequestLogMessage { get; init; } = "remote json request";
    public string SuccessLogMessagePrefix { get; init; } = "remote json success";
    public string RetryLogMessagePrefix { get; init; } = "remote json retry scheduled";
    public string FailureLogMessagePrefix { get; init; } = "remote json failed";
    public string HttpErrorPrefix { get; init; } = "http_";
    public string TimeoutErrorCode { get; init; } = "timeout";
    public string RequestFailedErrorCode { get; init; } = "request_failed";
    public string EmptyResponseErrorCode { get; init; } = "empty_response";
    public string UnexpectedErrorCode { get; init; } = "unexpected_error";
    public string CanceledErrorCode { get; init; } = "canceled";
}

public sealed class RemoteJsonFetcher
{
    public const int DefaultMaxAttempts = 2;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(300);

    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _timeout;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public RemoteJsonFetcher(
        HttpClient httpClient,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? retryDelay = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? DefaultTimeout;
        _maxAttempts = Math.Max(1, maxAttempts);
        _retryDelay = retryDelay ?? DefaultRetryDelay;
    }

    public async Task<RemoteJsonFetchResult> FetchStringAsync(
        Func<HttpRequestMessage> createRequest,
        RemoteJsonFetchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createRequest);
        return await FetchStringAsync(
            _ => Task.FromResult(createRequest()),
            options,
            cancellationToken);
    }

    public async Task<RemoteJsonFetchResult> FetchStringAsync(
        Func<CancellationToken, Task<HttpRequestMessage>> createRequest,
        RemoteJsonFetchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createRequest);
        var safeOptions = options ?? new RemoteJsonFetchOptions();

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                _logger.Debug(safeOptions.LogCategory, $"{safeOptions.RequestLogMessage} attempt={attempt} max_attempts={_maxAttempts}");
                _logger.Info(safeOptions.LogCategory, safeOptions.RequestLogMessage);
                using var request = await createRequest(cancellationToken).ConfigureAwait(false);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_timeout);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                _logger.Debug(safeOptions.LogCategory, $"{safeOptions.RequestLogMessage} response_status={(int)response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"{safeOptions.HttpErrorPrefix}{(int)response.StatusCode}";
                    if (ShouldRetry(errorCode, safeOptions.HttpErrorPrefix, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, safeOptions, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error(safeOptions.LogCategory, $"{safeOptions.FailureLogMessagePrefix} code={errorCode}");
                    return RemoteJsonFetchResult.Failure(errorCode);
                }

                var headers = CreateHeadersSnapshot(response);
                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                if (string.IsNullOrWhiteSpace(content))
                {
                    var errorCode = safeOptions.EmptyResponseErrorCode;
                    if (ShouldRetry(errorCode, safeOptions.HttpErrorPrefix, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, safeOptions, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error(safeOptions.LogCategory, $"{safeOptions.FailureLogMessagePrefix} code={errorCode}");
                    return RemoteJsonFetchResult.Failure(errorCode);
                }

                _logger.Info(safeOptions.LogCategory, $"{safeOptions.SuccessLogMessagePrefix} bytes={content.Length}");
                return RemoteJsonFetchResult.Success(content, headers);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning(safeOptions.LogCategory, $"{safeOptions.FailureLogMessagePrefix} code={safeOptions.CanceledErrorCode}");
                return RemoteJsonFetchResult.Failure(safeOptions.CanceledErrorCode);
            }
            catch (OperationCanceledException)
            {
                var errorCode = safeOptions.TimeoutErrorCode;
                if (ShouldRetry(errorCode, safeOptions.HttpErrorPrefix, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, safeOptions, cancellationToken))
                {
                    continue;
                }

                _logger.Warning(safeOptions.LogCategory, $"{safeOptions.FailureLogMessagePrefix} code={errorCode}");
                return RemoteJsonFetchResult.Failure(errorCode);
            }
            catch (HttpRequestException ex)
            {
                var errorCode = safeOptions.RequestFailedErrorCode;
                if (ShouldRetry(errorCode, safeOptions.HttpErrorPrefix, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, safeOptions, cancellationToken))
                {
                    continue;
                }

                var statusText = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
                var innerType = ex.InnerException?.GetType().Name ?? "none";
                _logger.Error(
                    safeOptions.LogCategory,
                    $"{safeOptions.FailureLogMessagePrefix} code={errorCode} status={statusText} inner={innerType}",
                    ex);
                return RemoteJsonFetchResult.Failure(errorCode);
            }
            catch (Exception ex)
            {
                var errorCode = safeOptions.UnexpectedErrorCode;
                if (ShouldRetry(errorCode, safeOptions.HttpErrorPrefix, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, safeOptions, cancellationToken))
                {
                    continue;
                }

                _logger.Error(safeOptions.LogCategory, $"{safeOptions.FailureLogMessagePrefix} code={errorCode}", ex);
                return RemoteJsonFetchResult.Failure(errorCode);
            }
        }

        return RemoteJsonFetchResult.Failure(NormalizeStatusCode(options?.RequestFailedErrorCode, "request_failed"));
    }

    private bool ShouldRetry(string errorCode, string httpErrorPrefix, int attempt)
    {
        if (attempt >= _maxAttempts)
        {
            return false;
        }

        if (TryParseHttpStatusCode(errorCode, httpErrorPrefix, out var statusCode))
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        return errorCode.EndsWith("timeout", StringComparison.Ordinal)
               || errorCode.EndsWith("request_failed", StringComparison.Ordinal)
               || errorCode.EndsWith("empty_response", StringComparison.Ordinal)
               || errorCode.EndsWith("unexpected_error", StringComparison.Ordinal);
    }

    private static bool TryParseHttpStatusCode(string errorCode, string prefix, out int statusCode)
    {
        statusCode = 0;
        if (string.IsNullOrWhiteSpace(errorCode)
            || string.IsNullOrWhiteSpace(prefix)
            || !errorCode.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(errorCode[prefix.Length..], out statusCode);
    }

    private async Task<bool> TryDelayBeforeRetryAsync(
        int attempt,
        string errorCode,
        RemoteJsonFetchOptions options,
        CancellationToken cancellationToken)
    {
        var nextAttempt = attempt + 1;
        _logger.Warning(options.LogCategory, $"{options.RetryLogMessagePrefix} attempt={nextAttempt} code={errorCode}");

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

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CreateHeadersSnapshot(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        return headers;
    }
}

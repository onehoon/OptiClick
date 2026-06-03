using System.Net.Http;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Downloads;

public sealed class RemoteRuntimeDataClient : IRemoteRuntimeDataClient
{
    private const string UserAgentProduct = "OptiClick";
    private readonly IRemoteEndpointProvider _endpointProvider;
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;
    private readonly Func<string?>? _appVersionProvider;
    private readonly TimeSpan _timeout;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public RemoteRuntimeDataClient(
        IRemoteEndpointProvider endpointProvider,
        HttpClient httpClient,
        IAppLogger? logger = null,
        Func<string?>? appVersionProvider = null,
        TimeSpan? timeout = null,
        int maxAttempts = 3,
        TimeSpan? retryDelay = null)
    {
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullAppLogger.Instance;
        _appVersionProvider = appVersionProvider;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _maxAttempts = Math.Max(1, maxAttempts);
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public async Task<RemoteRuntimeDataFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var options = _endpointProvider.GetRemoteDataOptions();
        var endpoint = options.GetEffectiveRuntimeDataUrl();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.Warning("Remote", "runtime-data fetch skipped reason=runtime_data_endpoint_missing");
            return RemoteRuntimeDataFetchResult.Skipped();
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            _logger.Error("Remote", "runtime-data fetch failed code=invalid_endpoint");
            return RemoteRuntimeDataFetchResult.Failure("invalid_endpoint");
        }

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                _logger.Info("Remote", "runtime-data fetch start endpoint=configured");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
                request.Headers.Accept.ParseAdd("application/json");
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"http_{(int)response.StatusCode}";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("Remote", $"runtime-data fetch failed code={errorCode}");
                    return RemoteRuntimeDataFetchResult.Failure(errorCode);
                }

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                if (string.IsNullOrWhiteSpace(content))
                {
                    const string errorCode = "empty_response";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("Remote", "runtime-data fetch failed code=empty_response");
                    return RemoteRuntimeDataFetchResult.Failure(errorCode);
                }

                _logger.Info("Remote", $"runtime-data fetch success bytes={content.Length}");
                return RemoteRuntimeDataFetchResult.Success(content);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("Remote", "runtime-data fetch failed code=canceled");
                return RemoteRuntimeDataFetchResult.Failure("canceled");
            }
            catch (OperationCanceledException)
            {
                const string errorCode = "timeout";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Warning("Remote", "runtime-data fetch failed code=timeout");
                return RemoteRuntimeDataFetchResult.Failure(errorCode);
            }
            catch (HttpRequestException ex)
            {
                const string errorCode = "request_failed";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                var statusText = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
                var innerType = ex.InnerException?.GetType().Name ?? "none";
                _logger.Error(
                    "Remote",
                    $"runtime-data fetch failed code=request_failed status={statusText} inner={innerType}",
                    ex);
                return RemoteRuntimeDataFetchResult.Failure(errorCode);
            }
            catch (Exception ex)
            {
                const string errorCode = "unexpected_error";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("Remote", "runtime-data fetch failed code=unexpected_error", ex);
                return RemoteRuntimeDataFetchResult.Failure(errorCode);
            }
        }

        return RemoteRuntimeDataFetchResult.Failure("request_failed");
    }

    private string BuildUserAgentValue()
    {
        var rawVersion = _appVersionProvider?.Invoke();
        var normalizedVersion = (rawVersion ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedVersion))
        {
            normalizedVersion = "0.0.0";
        }

        return $"{UserAgentProduct}/{normalizedVersion}";
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
        _logger.Warning("Remote", $"runtime-data fetch retry scheduled attempt={nextAttempt} code={errorCode}");

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
}

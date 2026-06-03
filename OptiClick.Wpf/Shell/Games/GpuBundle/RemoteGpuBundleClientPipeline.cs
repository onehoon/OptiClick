using System.Net.Http;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class RemoteGpuBundleFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Content { get; init; } = "";

    public static RemoteGpuBundleFetchResult Success(string content)
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = true,
            Content = content ?? ""
        };
    }

    public static RemoteGpuBundleFetchResult Skipped()
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = false,
            IsSkipped = true
        };
    }

    public static RemoteGpuBundleFetchResult Failure(string errorCode)
    {
        return new RemoteGpuBundleFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public sealed class GpuBundleFetchRequest
{
    public string Vendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string RequestSource { get; init; } = "";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string AppVersion { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
}

public sealed class GpuBundleUnsupportedReportRequest
{
    public string Vendor { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string RequestSource { get; init; } = "app";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string AppVersion { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
    public string Reason { get; init; } = "manifest_no_match";
}

public interface IGpuBundleRequestUriBuilder
{
    Uri? Build(string endpoint, GpuBundleFetchRequest request);
}

public sealed class GpuBundleRequestUriBuilder : IGpuBundleRequestUriBuilder
{
    public Uri? Build(string endpoint, GpuBundleFetchRequest request)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        var vendor = (request?.Vendor ?? "").Trim();
        var bundleKey = (request?.BundleKey ?? "").Trim();
        if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(bundleKey))
        {
            return null;
        }

        var queryPairs = new List<(string Key, string Value)>
        {
            ("vendor", vendor),
            ("bundle", bundleKey),
            ("gpu_raw", (request?.GpuRaw ?? "").Trim()),
            ("request_source", (request?.RequestSource ?? "").Trim()),
            ("device_manufacturer", (request?.DeviceManufacturer ?? "").Trim()),
            ("device_model", (request?.DeviceModel ?? "").Trim()),
            ("app_version", (request?.AppVersion ?? "").Trim()),
            ("manifest_version", (request?.ManifestVersion ?? "").Trim())
        };

        var uriBuilder = new UriBuilder(baseUri)
        {
            Query = MergeQuery(baseUri.Query, queryPairs)
        };
        return uriBuilder.Uri;
    }

    private static string MergeQuery(string existingQuery, IReadOnlyList<(string Key, string Value)> pairs)
    {
        var normalizedExisting = (existingQuery ?? "").Trim();
        if (normalizedExisting.StartsWith("?", StringComparison.Ordinal))
        {
            normalizedExisting = normalizedExisting[1..];
        }

        var encoded = pairs
            .Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")
            .ToArray();
        if (encoded.Length == 0)
        {
            return normalizedExisting;
        }

        if (string.IsNullOrWhiteSpace(normalizedExisting))
        {
            return string.Join("&", encoded);
        }

        return normalizedExisting + "&" + string.Join("&", encoded);
    }
}

public interface IRemoteGpuBundleClient
{
    Task<RemoteGpuBundleFetchResult> FetchAsync(
        string endpoint,
        GpuBundleFetchRequest request,
        CancellationToken cancellationToken = default);

    Task<RemoteGpuBundleFetchResult> ReportUnsupportedAsync(
        string endpoint,
        GpuBundleUnsupportedReportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteGpuBundleClient : IRemoteGpuBundleClient
{
    private readonly HttpClient _httpClient;
    private readonly IGpuBundleRequestUriBuilder _requestUriBuilder;
    private readonly TimeSpan _timeout;
    private readonly IAppLogger _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public RemoteGpuBundleClient(
        HttpClient httpClient,
        IGpuBundleRequestUriBuilder requestUriBuilder,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        int maxAttempts = 3,
        TimeSpan? retryDelay = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestUriBuilder = requestUriBuilder ?? throw new ArgumentNullException(nameof(requestUriBuilder));
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
        _maxAttempts = Math.Max(1, maxAttempts);
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public async Task<RemoteGpuBundleFetchResult> FetchAsync(
        string endpoint,
        GpuBundleFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            _logger.Warning("remote", "gpu-bundle skipped code=bundle_endpoint_missing");
            return RemoteGpuBundleFetchResult.Skipped();
        }

        var requestUri = _requestUriBuilder.Build(normalizedEndpoint, request);
        if (requestUri is null)
        {
            _logger.Error("remote", "gpu-bundle failed code=invalid_bundle_endpoint");
            return RemoteGpuBundleFetchResult.Failure("invalid_bundle_endpoint");
        }

        var safeRequest = request ?? new GpuBundleFetchRequest();
        _logger.Info(
            "remote",
            $"gpu-bundle request vendor={NormalizeLogValue(safeRequest.Vendor, "none")} bundle={NormalizeLogValue(safeRequest.BundleKey, "none")} gpu_raw={NormalizeLogValue(safeRequest.GpuRaw, "none")} request_source={NormalizeLogValue(safeRequest.RequestSource, "none")} device_manufacturer={NormalizeLogValue(safeRequest.DeviceManufacturer, "none")} device_model={NormalizeLogValue(safeRequest.DeviceModel, "none")} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version={NormalizeLogValue(safeRequest.ManifestVersion, "none")}");

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"bundle_http_{(int)response.StatusCode}";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("remote", $"gpu-bundle failed code={errorCode}");
                    return RemoteGpuBundleFetchResult.Failure(errorCode);
                }

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                if (string.IsNullOrWhiteSpace(content))
                {
                    const string errorCode = "empty_bundle_response";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("remote", "gpu-bundle failed code=empty_bundle_response");
                    return RemoteGpuBundleFetchResult.Failure(errorCode);
                }

                _logger.Info("remote", $"gpu-bundle success bytes={content.Length}");

                return RemoteGpuBundleFetchResult.Success(content);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("remote", "gpu-bundle canceled code=bundle_canceled");
                return RemoteGpuBundleFetchResult.Failure("bundle_canceled");
            }
            catch (OperationCanceledException)
            {
                const string errorCode = "bundle_timeout";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle failed code=bundle_timeout");
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
            catch (HttpRequestException ex)
            {
                const string errorCode = "bundle_request_failed";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                var status = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
                _logger.Error("remote", $"gpu-bundle failed code=bundle_request_failed status={status}", ex);
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
            catch (Exception ex)
            {
                const string errorCode = "bundle_unexpected_error";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle failed code=bundle_unexpected_error", ex);
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
        }

        return RemoteGpuBundleFetchResult.Failure("bundle_request_failed");
    }

    public async Task<RemoteGpuBundleFetchResult> ReportUnsupportedAsync(
        string endpoint,
        GpuBundleUnsupportedReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            _logger.Warning("remote", "gpu-bundle-report skipped code=bundle_endpoint_missing");
            return RemoteGpuBundleFetchResult.Skipped();
        }

        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var baseUri))
        {
            _logger.Error("remote", "gpu-bundle-report failed code=invalid_bundle_endpoint");
            return RemoteGpuBundleFetchResult.Failure("invalid_bundle_endpoint");
        }

        var safeRequest = request ?? new GpuBundleUnsupportedReportRequest();
        var requestUri = BuildUnsupportedReportUri(baseUri, safeRequest);
        _logger.Info(
            "remote",
            $"gpu-bundle-report request vendor={NormalizeLogValue(safeRequest.Vendor, "none")} bundle=unknown gpu_group=unknown gpu_raw={NormalizeLogValue(safeRequest.GpuRaw, "none")} request_source={NormalizeLogValue(safeRequest.RequestSource, "none")} device_manufacturer={NormalizeLogValue(safeRequest.DeviceManufacturer, "none")} device_model={NormalizeLogValue(safeRequest.DeviceModel, "none")} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version={NormalizeLogValue(safeRequest.ManifestVersion, "none")} report_only=1 reason={NormalizeLogValue(safeRequest.Reason, "manifest_no_match")}");

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"bundle_report_http_{(int)response.StatusCode}";
                    if (ShouldRetryReport(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("remote", $"gpu-bundle-report failed code={errorCode}");
                    return RemoteGpuBundleFetchResult.Failure(errorCode);
                }

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                _logger.Info("remote", $"gpu-bundle-report success status={(int)response.StatusCode}");
                return RemoteGpuBundleFetchResult.Success(content ?? "");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("remote", "gpu-bundle-report canceled code=bundle_report_canceled");
                return RemoteGpuBundleFetchResult.Failure("bundle_report_canceled");
            }
            catch (OperationCanceledException)
            {
                const string errorCode = "bundle_report_timeout";
                if (ShouldRetryReport(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle-report failed code=bundle_report_timeout");
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
            catch (HttpRequestException ex)
            {
                const string errorCode = "bundle_report_request_failed";
                if (ShouldRetryReport(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                var status = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
                _logger.Error("remote", $"gpu-bundle-report failed code=bundle_report_request_failed status={status}", ex);
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
            catch (Exception ex)
            {
                const string errorCode = "bundle_report_unexpected_error";
                if (ShouldRetryReport(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle-report failed code=bundle_report_unexpected_error", ex);
                return RemoteGpuBundleFetchResult.Failure(errorCode);
            }
        }

        return RemoteGpuBundleFetchResult.Failure("bundle_report_request_failed");
    }

    private static Uri BuildUnsupportedReportUri(Uri baseUri, GpuBundleUnsupportedReportRequest request)
    {
        var reason = (request.Reason ?? "").Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = "manifest_no_match";
        }

        var queryPairs = new List<(string Key, string Value)>
        {
            ("vendor", (request.Vendor ?? "").Trim()),
            ("bundle", "unknown"),
            ("gpu_group", "unknown"),
            ("gpu_raw", (request.GpuRaw ?? "").Trim()),
            ("request_source", (request.RequestSource ?? "").Trim()),
            ("device_manufacturer", (request.DeviceManufacturer ?? "").Trim()),
            ("device_model", (request.DeviceModel ?? "").Trim()),
            ("app_version", (request.AppVersion ?? "").Trim()),
            ("manifest_version", (request.ManifestVersion ?? "").Trim()),
            ("report_only", "1"),
            ("reason", reason)
        };

        var uriBuilder = new UriBuilder(baseUri)
        {
            Query = MergeQuery(baseUri.Query, queryPairs)
        };
        return uriBuilder.Uri;
    }

    private static string MergeQuery(string existingQuery, IReadOnlyList<(string Key, string Value)> pairs)
    {
        var normalizedExisting = (existingQuery ?? "").Trim();
        if (normalizedExisting.StartsWith("?", StringComparison.Ordinal))
        {
            normalizedExisting = normalizedExisting[1..];
        }

        var encoded = pairs
            .Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")
            .ToArray();
        if (encoded.Length == 0)
        {
            return normalizedExisting;
        }

        if (string.IsNullOrWhiteSpace(normalizedExisting))
        {
            return string.Join("&", encoded);
        }

        return normalizedExisting + "&" + string.Join("&", encoded);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private bool ShouldRetry(string errorCode, int attempt)
    {
        if (attempt >= _maxAttempts)
        {
            return false;
        }

        if (TryParseHttpStatusCode(errorCode, "bundle_http_", out var statusCode))
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        return string.Equals(errorCode, "bundle_timeout", StringComparison.Ordinal)
               || string.Equals(errorCode, "bundle_request_failed", StringComparison.Ordinal)
               || string.Equals(errorCode, "empty_bundle_response", StringComparison.Ordinal)
               || string.Equals(errorCode, "bundle_unexpected_error", StringComparison.Ordinal);
    }

    private bool ShouldRetryReport(string errorCode, int attempt)
    {
        if (attempt >= _maxAttempts)
        {
            return false;
        }

        if (TryParseHttpStatusCode(errorCode, "bundle_report_http_", out var statusCode))
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        return string.Equals(errorCode, "bundle_report_timeout", StringComparison.Ordinal)
               || string.Equals(errorCode, "bundle_report_request_failed", StringComparison.Ordinal)
               || string.Equals(errorCode, "bundle_report_unexpected_error", StringComparison.Ordinal);
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
        CancellationToken cancellationToken)
    {
        var nextAttempt = attempt + 1;
        _logger.Warning("remote", $"gpu-bundle retry scheduled attempt={nextAttempt} code={errorCode}");

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



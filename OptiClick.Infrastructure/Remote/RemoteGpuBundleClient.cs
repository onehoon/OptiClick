using System.Net.Http;
using OptiClick.Core.Games.GpuBundle;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;

namespace OptiClick.Infrastructure.Remote;

public sealed class RemoteGpuBundleClient : IRemoteGpuBundleClient
{
    private const string UserAgentProduct = "OptiClick";

    private readonly HttpClient _httpClient;
    private readonly IGpuBundleRequestUriBuilder _requestUriBuilder;
    private readonly TimeSpan _timeout;
    private readonly IAppLogger _logger;
    private readonly Func<string?>? _appVersionProvider;
    private readonly IOptiClickApiRequestAuthenticator? _authenticator;
    private readonly IOptiClickApiTicketStore? _ticketStore;
    private readonly IOptiClickServerClock? _serverClock;
    private readonly RemoteJsonFetcher _jsonFetcher;

    public RemoteGpuBundleClient(
        HttpClient httpClient,
        IGpuBundleRequestUriBuilder requestUriBuilder,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        int maxAttempts = RemoteJsonFetcher.DefaultMaxAttempts,
        TimeSpan? retryDelay = null,
        Func<string?>? appVersionProvider = null,
        IOptiClickApiRequestAuthenticator? authenticator = null,
        IOptiClickApiTicketStore? ticketStore = null,
        IOptiClickServerClock? serverClock = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestUriBuilder = requestUriBuilder ?? throw new ArgumentNullException(nameof(requestUriBuilder));
        _logger = logger ?? NullAppLogger.Instance;
        _appVersionProvider = appVersionProvider;
        _authenticator = authenticator;
        _ticketStore = ticketStore;
        _serverClock = serverClock;
        _timeout = timeout ?? RemoteJsonFetcher.DefaultTimeout;
        _jsonFetcher = new RemoteJsonFetcher(
            _httpClient,
            _logger,
            timeout,
            maxAttempts,
            retryDelay,
            _serverClock);
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
            $"gpu-bundle request vendor={NormalizeLogValue(safeRequest.Vendor, "none")} bundle={NormalizeLogValue(safeRequest.BundleKey, "none")} gpu_raw={NormalizeLogValue(safeRequest.GpuRaw, "none")} request_source={NormalizeLogValue(safeRequest.RequestSource, "none")} device_manufacturer={NormalizeLogValue(safeRequest.DeviceManufacturer, "none")} device_model={NormalizeLogValue(safeRequest.DeviceModel, "none")} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version={NormalizeLogValue(safeRequest.ManifestVersion, "none")} device_info_source={NormalizeLogValue(safeRequest.DeviceInfoSource, "none")} gpu_info_source={NormalizeLogValue(safeRequest.GpuInfoSource, "none")} wmi_device_status={NormalizeLogValue(safeRequest.WmiDeviceStatus, "none")} wmi_gpu_status={NormalizeLogValue(safeRequest.WmiGpuStatus, "none")} wmi_device_attempts={safeRequest.WmiDeviceAttempts} wmi_gpu_attempts={safeRequest.WmiGpuAttempts}");
        _logger.Debug(
            "Security",
            $"gpu-bundle security context authenticator_configured={FormatBool(_authenticator is not null)} bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(_ticketStore?.BundleTicket))} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version_present={FormatBool(!string.IsNullOrWhiteSpace(safeRequest.ManifestVersion))}");

        var fetchResult = await _jsonFetcher.FetchStringAsync(
            cancellationToken => CreateJsonRequestAsync(
                requestUri,
                safeRequest.AppVersion,
                safeRequest.ManifestVersion,
                _ticketStore?.BundleTicket ?? "",
                cancellationToken),
            new RemoteJsonFetchOptions
            {
                LogCategory = "remote",
                RequestLogMessage = "gpu-bundle fetch start",
                SuccessLogMessagePrefix = "gpu-bundle success",
                RetryLogMessagePrefix = "gpu-bundle retry scheduled",
                FailureLogMessagePrefix = "gpu-bundle failed",
                HttpErrorPrefix = "bundle_http_",
                TimeoutErrorCode = "bundle_timeout",
                RequestFailedErrorCode = "bundle_request_failed",
                EmptyResponseErrorCode = "empty_bundle_response",
                UnexpectedErrorCode = "bundle_unexpected_error",
                CanceledErrorCode = "bundle_canceled"
            },
            cancellationToken);

        return fetchResult.IsSuccess
            ? RemoteGpuBundleFetchResult.Success(fetchResult.Content)
            : RemoteGpuBundleFetchResult.Failure(fetchResult.ErrorCode);
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
            $"gpu-bundle-report request vendor={NormalizeLogValue(safeRequest.Vendor, "none")} bundle=unknown gpu_group=unknown gpu_raw={NormalizeLogValue(safeRequest.GpuRaw, "none")} request_source={NormalizeLogValue(safeRequest.RequestSource, "none")} device_manufacturer={NormalizeLogValue(safeRequest.DeviceManufacturer, "none")} device_model={NormalizeLogValue(safeRequest.DeviceModel, "none")} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version={NormalizeLogValue(safeRequest.ManifestVersion, "none")} report_only=1 reason={NormalizeLogValue(safeRequest.Reason, "manifest_no_match")} device_info_source={NormalizeLogValue(safeRequest.DeviceInfoSource, "none")} gpu_info_source={NormalizeLogValue(safeRequest.GpuInfoSource, "none")} wmi_device_status={NormalizeLogValue(safeRequest.WmiDeviceStatus, "none")} wmi_gpu_status={NormalizeLogValue(safeRequest.WmiGpuStatus, "none")} wmi_device_attempts={safeRequest.WmiDeviceAttempts} wmi_gpu_attempts={safeRequest.WmiGpuAttempts}");
        _logger.Debug(
            "Security",
            $"gpu-bundle-report security context authenticator_configured={FormatBool(_authenticator is not null)} bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(_ticketStore?.BundleTicket))} app_version={NormalizeLogValue(safeRequest.AppVersion, "none")} manifest_version_present={FormatBool(!string.IsNullOrWhiteSpace(safeRequest.ManifestVersion))}");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        var timestampSkewRecoveryRetried = false;
        try
        {
            while (true)
            {
                using var httpRequest = await CreateJsonRequestAsync(
                    requestUri,
                    safeRequest.AppVersion,
                    safeRequest.ManifestVersion,
                    _ticketStore?.BundleTicket ?? "",
                    timeoutCts.Token).ConfigureAwait(false);
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);
                response.RequestMessage ??= httpRequest;

                var clockAdjusted = _serverClock?.Observe(response) == true;
                if (OptiClickTimestampSkewRecovery.ShouldRetryAfterClockAdjustment(
                        httpRequest,
                        response,
                        clockAdjusted,
                        timestampSkewRecoveryRetried))
                {
                    timestampSkewRecoveryRetried = true;
                    _logger.Warning(
                        "remote",
                        $"remote retry reason=timestamp_skew_recovery path={OptiClickTimestampSkewRecovery.ResolvePathForLog(httpRequest)}");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"bundle_report_http_{(int)response.StatusCode}";
                    _logger.Error("remote", $"gpu-bundle-report failed code={errorCode}");
                    return RemoteGpuBundleFetchResult.Failure(errorCode);
                }

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                _logger.Info("remote", $"gpu-bundle-report success status={(int)response.StatusCode}");
                return RemoteGpuBundleFetchResult.Success(content ?? "");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("remote", "gpu-bundle-report canceled code=bundle_report_canceled");
            return RemoteGpuBundleFetchResult.Failure("bundle_report_canceled");
        }
        catch (OperationCanceledException)
        {
            _logger.Error("remote", "gpu-bundle-report failed code=bundle_report_timeout");
            return RemoteGpuBundleFetchResult.Failure("bundle_report_timeout");
        }
        catch (HttpRequestException ex)
        {
            var status = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
            _logger.Error("remote", $"gpu-bundle-report failed code=bundle_report_request_failed status={status}", ex);
            return RemoteGpuBundleFetchResult.Failure("bundle_report_request_failed");
        }
        catch (Exception ex)
        {
            _logger.Error("remote", "gpu-bundle-report failed code=bundle_report_unexpected_error", ex);
            return RemoteGpuBundleFetchResult.Failure("bundle_report_unexpected_error");
        }
    }

    private async Task<HttpRequestMessage> CreateJsonRequestAsync(
        Uri requestUri,
        string appVersion,
        string manifestVersion,
        string bundleTicket,
        CancellationToken cancellationToken)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
        httpRequest.Headers.Accept.ParseAdd("application/json");
        if (_authenticator is not null)
        {
            _logger.Debug(
                "Security",
                $"gpu-bundle auth apply app_version={NormalizeLogValue(appVersion, ResolveAppVersion())} manifest_version_present={FormatBool(!string.IsNullOrWhiteSpace(manifestVersion))} bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(bundleTicket))}");
            await _authenticator.ApplyAsync(
                httpRequest,
                new OptiClickApiRequestContext
                {
                    AppVersion = string.IsNullOrWhiteSpace(appVersion)
                        ? ResolveAppVersion()
                        : (appVersion ?? "").Trim(),
                    ManifestVersion = (manifestVersion ?? "").Trim(),
                    BundleTicket = (bundleTicket ?? "").Trim()
                },
                cancellationToken).ConfigureAwait(false);
        }

        return httpRequest;
    }

    private string BuildUserAgentValue()
    {
        var normalizedVersion = ResolveAppVersion();
        if (string.IsNullOrWhiteSpace(normalizedVersion))
        {
            normalizedVersion = "0.0.0";
        }

        return $"{UserAgentProduct}/{normalizedVersion}";
    }

    private string ResolveAppVersion()
    {
        return (_appVersionProvider?.Invoke() ?? "").Trim();
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
            ("reason", reason),
            ("device_info_source", (request.DeviceInfoSource ?? "").Trim()),
            ("gpu_info_source", (request.GpuInfoSource ?? "").Trim()),
            ("wmi_device_status", (request.WmiDeviceStatus ?? "").Trim()),
            ("wmi_gpu_status", (request.WmiGpuStatus ?? "").Trim()),
            ("wmi_device_attempts", Math.Max(0, request.WmiDeviceAttempts).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("wmi_gpu_attempts", Math.Max(0, request.WmiGpuAttempts).ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        return RemoteRequestUriQueryBuilder.Build(baseUri, queryPairs);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }
}

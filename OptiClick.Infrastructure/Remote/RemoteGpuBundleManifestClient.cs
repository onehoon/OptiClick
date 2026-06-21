using System.Net.Http;
using OptiClick.Core.Games.GpuBundle;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;

namespace OptiClick.Infrastructure.Remote;

public sealed class RemoteGpuBundleManifestClient : IRemoteGpuBundleManifestClient
{
    private const string UserAgentProduct = "OptiClick";

    private readonly HttpClient _httpClient;
    private readonly IGpuBundleManifestRequestUriBuilder _requestUriBuilder;
    private readonly IRemoteGpuBundleManifestParser _parser;
    private readonly IAppLogger _logger;
    private readonly Func<string?>? _appVersionProvider;
    private readonly IOptiClickApiRequestAuthenticator? _authenticator;
    private readonly IOptiClickApiTicketStore? _ticketStore;
    private readonly IOptiClickServerClock? _serverClock;
    private readonly RemoteJsonFetcher _jsonFetcher;

    public RemoteGpuBundleManifestClient(
        HttpClient httpClient,
        IGpuBundleManifestRequestUriBuilder requestUriBuilder,
        IRemoteGpuBundleManifestParser parser,
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
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? NullAppLogger.Instance;
        _appVersionProvider = appVersionProvider;
        _authenticator = authenticator;
        _ticketStore = ticketStore;
        _serverClock = serverClock;
        _jsonFetcher = new RemoteJsonFetcher(
            _httpClient,
            _logger,
            timeout,
            maxAttempts,
            retryDelay,
            _serverClock);
    }

    public async Task<RemoteGpuBundleManifestFetchResult> FetchAsync(
        string endpoint,
        GpuBundleManifestFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            _logger.Warning("remote", "gpu-bundle-manifest skipped code=manifest_endpoint_missing");
            return RemoteGpuBundleManifestFetchResult.Skipped();
        }

        var requestUri = _requestUriBuilder.Build(normalizedEndpoint, request);
        if (requestUri is null)
        {
            _logger.Error("remote", "gpu-bundle-manifest failed code=invalid_manifest_endpoint");
            return RemoteGpuBundleManifestFetchResult.Failure("invalid_manifest_endpoint");
        }

        _logger.Debug(
            "Security",
            $"gpu-bundle-manifest security context authenticator_configured={FormatBool(_authenticator is not null)} ticket_store_configured={FormatBool(_ticketStore is not null)} app_version={NormalizeLogValue(ResolveAppVersion(), "none")}");
        var fetchResult = await _jsonFetcher.FetchStringAsync(
            cancellationToken => CreateJsonRequestAsync(requestUri, cancellationToken),
            new RemoteJsonFetchOptions
            {
                LogCategory = "remote",
                RequestLogMessage = "gpu-bundle-manifest request url_set=true",
                SuccessLogMessagePrefix = "gpu-bundle-manifest success",
                RetryLogMessagePrefix = "gpu-bundle-manifest retry scheduled",
                FailureLogMessagePrefix = "gpu-bundle-manifest failed",
                HttpErrorPrefix = "manifest_http_",
                TimeoutErrorCode = "manifest_timeout",
                RequestFailedErrorCode = "manifest_request_failed",
                EmptyResponseErrorCode = "empty_manifest_response",
                UnexpectedErrorCode = "manifest_unexpected_error",
                CanceledErrorCode = "manifest_canceled"
            },
            cancellationToken);

        if (!fetchResult.IsSuccess)
        {
            return RemoteGpuBundleManifestFetchResult.Failure(fetchResult.ErrorCode);
        }

        var parsed = _parser.Parse(fetchResult.Content);
        if (!parsed.IsSuccess)
        {
            _logger.Error("remote", $"gpu-bundle-manifest failed code={NormalizeLogValue(parsed.ErrorCode, "manifest_parse_failed")}");
            return RemoteGpuBundleManifestFetchResult.Failure(parsed.ErrorCode);
        }

        var bundleTicket = ReadHeader(fetchResult, OptiClickProtocolHeaders.BundleTicket);
        if (_ticketStore is not null)
        {
            _ticketStore.SetBundleTicket(bundleTicket);
        }

        var policyRevision = ReadHeader(fetchResult, "X-OptiClick-Policy-Revision");
        _logger.Debug(
            "Security",
            $"gpu-bundle-manifest ticket received bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(bundleTicket))} policy_revision={NormalizeLogValue(policyRevision, "none")}");
        _logger.Info("remote", $"gpu-bundle-manifest parsed rules={parsed.Manifest.Rules.Count} manifest_version={NormalizeLogValue(parsed.Manifest.ManifestVersion, "none")}");
        return RemoteGpuBundleManifestFetchResult.Success(parsed.Manifest, bundleTicket, policyRevision);
    }

    private async Task<HttpRequestMessage> CreateJsonRequestAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
        httpRequest.Headers.Accept.ParseAdd("application/json");
        if (_authenticator is not null)
        {
            _logger.Debug(
                "Security",
                $"gpu-bundle-manifest auth apply app_version={NormalizeLogValue(ResolveAppVersion(), "none")}");
            await _authenticator.ApplyAsync(
                httpRequest,
                new OptiClickApiRequestContext
                {
                    AppVersion = ResolveAppVersion()
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

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string ReadHeader(RemoteJsonFetchResult result, string headerName)
    {
        if (result.Headers.TryGetValue(headerName, out var values))
        {
            return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
        }

        return "";
    }
}

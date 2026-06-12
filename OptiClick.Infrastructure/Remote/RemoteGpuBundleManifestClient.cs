using System.Net.Http;
using OptiClick.Core.Games.GpuBundle;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Remote;

public sealed class RemoteGpuBundleManifestClient : IRemoteGpuBundleManifestClient
{
    private readonly HttpClient _httpClient;
    private readonly IGpuBundleManifestRequestUriBuilder _requestUriBuilder;
    private readonly IRemoteGpuBundleManifestParser _parser;
    private readonly IAppLogger _logger;
    private readonly RemoteJsonFetcher _jsonFetcher;

    public RemoteGpuBundleManifestClient(
        HttpClient httpClient,
        IGpuBundleManifestRequestUriBuilder requestUriBuilder,
        IRemoteGpuBundleManifestParser parser,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        int maxAttempts = RemoteJsonFetcher.DefaultMaxAttempts,
        TimeSpan? retryDelay = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestUriBuilder = requestUriBuilder ?? throw new ArgumentNullException(nameof(requestUriBuilder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? NullAppLogger.Instance;
        _jsonFetcher = new RemoteJsonFetcher(
            _httpClient,
            _logger,
            timeout,
            maxAttempts,
            retryDelay);
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

        var fetchResult = await _jsonFetcher.FetchStringAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
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

        _logger.Info("remote", $"gpu-bundle-manifest parsed rules={parsed.Manifest.Rules.Count} manifest_version={NormalizeLogValue(parsed.Manifest.ManifestVersion, "none")}");
        return RemoteGpuBundleManifestFetchResult.Success(parsed.Manifest);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

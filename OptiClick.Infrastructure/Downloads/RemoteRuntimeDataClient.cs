using System.Net.Http;
using OptiClick.Core.Abstractions;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Remote;

namespace OptiClick.Infrastructure.Downloads;

public sealed class RemoteRuntimeDataClient : IRemoteRuntimeDataClient
{
    private const string UserAgentProduct = "OptiClick";
    private readonly IRemoteEndpointProvider _endpointProvider;
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;
    private readonly Func<string?>? _appVersionProvider;
    private readonly RemoteJsonFetcher _jsonFetcher;

    public RemoteRuntimeDataClient(
        IRemoteEndpointProvider endpointProvider,
        HttpClient httpClient,
        IAppLogger? logger = null,
        Func<string?>? appVersionProvider = null,
        TimeSpan? timeout = null,
        int maxAttempts = RemoteJsonFetcher.DefaultMaxAttempts,
        TimeSpan? retryDelay = null)
    {
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullAppLogger.Instance;
        _appVersionProvider = appVersionProvider;
        _jsonFetcher = new RemoteJsonFetcher(
            _httpClient,
            _logger,
            timeout,
            maxAttempts,
            retryDelay);
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

        var fetchResult = await _jsonFetcher.FetchStringAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
                request.Headers.Accept.ParseAdd("application/json");
                return request;
            },
            new RemoteJsonFetchOptions
            {
                LogCategory = "Remote",
                RequestLogMessage = "runtime-data fetch start endpoint=configured",
                SuccessLogMessagePrefix = "runtime-data fetch success",
                RetryLogMessagePrefix = "runtime-data fetch retry scheduled",
                FailureLogMessagePrefix = "runtime-data fetch failed",
                HttpErrorPrefix = "http_",
                TimeoutErrorCode = "timeout",
                RequestFailedErrorCode = "request_failed",
                EmptyResponseErrorCode = "empty_response",
                UnexpectedErrorCode = "unexpected_error",
                CanceledErrorCode = "canceled"
            },
            cancellationToken);

        return fetchResult.IsSuccess
            ? RemoteRuntimeDataFetchResult.Success(fetchResult.Content)
            : RemoteRuntimeDataFetchResult.Failure(fetchResult.ErrorCode);
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
}

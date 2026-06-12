using OptiClick.Infrastructure.Downloads;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Runtime.DeviceIdentity;

public sealed class RemoteDeviceIdentityRulesLoader : IRemoteDeviceIdentityRulesLoader
{
    private readonly IRemoteDeviceIdentityRulesClient _client;
    private readonly IDeviceIdentityRulesParser _parser;
    private readonly IDeviceIdentityRulesProvider _rulesProvider;
    private readonly IDeviceIdentityRulesCacheStore? _cacheStore;
    private readonly IAppLogger _logger;

    public RemoteDeviceIdentityRulesLoader(
        IRemoteDeviceIdentityRulesClient client,
        IDeviceIdentityRulesParser parser,
        IDeviceIdentityRulesProvider rulesProvider,
        IDeviceIdentityRulesCacheStore? cacheStore = null,
        IAppLogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
        _cacheStore = cacheStore;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<RemoteDeviceIdentityRulesLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheApplied = TryApplyLocalCache();

            var fetchResult = await _client.FetchAsync(cancellationToken);
            if (fetchResult.IsSkipped)
            {
                return cacheApplied
                    ? RemoteDeviceIdentityRulesLoadResult.Success()
                    : RemoteDeviceIdentityRulesLoadResult.Skipped();
            }

            if (!fetchResult.IsSuccess)
            {
                if (cacheApplied)
                {
                    var code = NormalizeStatusCode(fetchResult.ErrorCode, "unknown");
                    _logger.Warning("device-rules", $"device identity rules remote refresh failed code={code} fallback=local_cache");
                    return RemoteDeviceIdentityRulesLoadResult.Success();
                }

                return RemoteDeviceIdentityRulesLoadResult.Failure(fetchResult.ErrorCode, fetchResult.ErrorMessage);
            }

            var parseResult = _parser.Parse(fetchResult.Content);
            if (!parseResult.IsSuccess)
            {
                if (cacheApplied)
                {
                    var code = NormalizeStatusCode(parseResult.ErrorCode, "invalid_remote_payload");
                    _logger.Warning("device-rules", $"device identity rules remote parse failed code={code} fallback=local_cache");
                    return RemoteDeviceIdentityRulesLoadResult.Success();
                }

                return RemoteDeviceIdentityRulesLoadResult.Failure(parseResult.ErrorCode, parseResult.ErrorMessage);
            }

            _rulesProvider.Update(parseResult.Rules);
            _cacheStore?.TryWriteContent(fetchResult.Content);
            return RemoteDeviceIdentityRulesLoadResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RemoteDeviceIdentityRulesLoadResult.Failure("canceled");
        }
        catch
        {
            return RemoteDeviceIdentityRulesLoadResult.Failure("unexpected_error");
        }
    }

    // Keep cache handling separate so startup can apply best-effort identity rules fast
    // and proceed without waiting for network round trips.
    public bool TryApplyLocalCache()
    {
        if (_cacheStore is null)
        {
            return false;
        }

        var cachedContent = _cacheStore.TryReadContent();
        if (string.IsNullOrWhiteSpace(cachedContent))
        {
            return false;
        }

        var parseResult = _parser.Parse(cachedContent);
        if (!parseResult.IsSuccess)
        {
            var code = NormalizeStatusCode(parseResult.ErrorCode, "invalid_cache_payload");
            _logger.Warning("device-rules", $"device identity rules cache parse failed code={code}");
            return false;
        }

        _rulesProvider.Update(parseResult.Rules);
        return true;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

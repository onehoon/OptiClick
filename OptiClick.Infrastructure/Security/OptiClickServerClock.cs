using System.Net;
using System.Net.Http;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Security;

public interface IOptiClickServerClock
{
    DateTimeOffset UtcNow { get; }

    bool Observe(HttpResponseMessage response);
}

public sealed class OptiClickServerClock : IOptiClickServerClock
{
    private static readonly TimeSpan DefaultAdjustmentThreshold = TimeSpan.FromSeconds(30);

    private readonly object _sync = new();
    private readonly HashSet<string> _trustedOriginKeys;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _adjustmentThreshold;
    private TimeSpan _offset = TimeSpan.Zero;

    public OptiClickServerClock(
        IEnumerable<Uri>? trustedWorkerEndpoints = null,
        Func<DateTimeOffset>? utcNow = null,
        IAppLogger? logger = null,
        TimeSpan? adjustmentThreshold = null)
    {
        _trustedOriginKeys = new HashSet<string>(
            (trustedWorkerEndpoints ?? Enumerable.Empty<Uri>())
                .Where(static endpoint => endpoint is not null && endpoint.IsAbsoluteUri)
                .Select(BuildOriginKey),
            StringComparer.OrdinalIgnoreCase);
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _logger = logger ?? NullAppLogger.Instance;
        _adjustmentThreshold = adjustmentThreshold ?? DefaultAdjustmentThreshold;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_sync)
            {
                return _utcNow() + _offset;
            }
        }
    }

    public bool Observe(HttpResponseMessage response)
    {
        if (response is null
            || !IsTrustedWorkerResponse(response)
            || response.Headers.Date is not { } serverDate)
        {
            return false;
        }

        var localUtcNow = _utcNow();
        var observedOffset = serverDate.ToUniversalTime() - localUtcNow;
        var observedSeconds = FormatSeconds(observedOffset);
        _logger.Debug("Security", $"server clock observed source=worker_date offset_seconds={observedSeconds}");

        lock (_sync)
        {
            if (Abs(observedOffset - _offset) < _adjustmentThreshold)
            {
                return false;
            }

            _offset = observedOffset;
        }

        _logger.Info("Security", $"server clock adjusted offset_seconds={observedSeconds}");
        return true;
    }

    private bool IsTrustedWorkerResponse(HttpResponseMessage response)
    {
        var requestUri = response.RequestMessage?.RequestUri;
        return requestUri is not null
               && IsWorkerApiPath(requestUri)
               && _trustedOriginKeys.Contains(BuildOriginKey(requestUri));
    }

    private static bool IsWorkerApiPath(Uri uri)
    {
        var path = (uri.AbsolutePath ?? "").TrimEnd('/');
        return string.Equals(path, "/v1", StringComparison.Ordinal)
               || path.StartsWith("/v1/", StringComparison.Ordinal);
    }

    private static string BuildOriginKey(Uri uri)
    {
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}:{ResolveEffectivePort(uri)}";
    }

    private static int ResolveEffectivePort(Uri uri)
    {
        if (!uri.IsDefaultPort)
        {
            return uri.Port;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            ? 80
            : 443;
    }

    private static TimeSpan Abs(TimeSpan value)
    {
        return value < TimeSpan.Zero ? value.Negate() : value;
    }

    private static long FormatSeconds(TimeSpan value)
    {
        return (long)Math.Round(value.TotalSeconds, MidpointRounding.AwayFromZero);
    }
}

internal static class OptiClickTimestampSkewRecovery
{
    public static bool ShouldRetryAfterClockAdjustment(
        HttpRequestMessage request,
        HttpResponseMessage response,
        bool clockAdjusted,
        bool alreadyRetried)
    {
        return !alreadyRetried
               && clockAdjusted
               && response.StatusCode == HttpStatusCode.NotFound
               && HasSignature(request);
    }

    public static bool HasSignature(HttpRequestMessage request)
    {
        return request.Headers.Contains(OptiClickProtocolHeaders.Signature);
    }

    public static string ResolvePathForLog(HttpRequestMessage request)
    {
        var path = (request.RequestUri?.AbsolutePath ?? "").Trim();
        return string.IsNullOrWhiteSpace(path) ? "none" : path;
    }

    public static string ResolveHostForLog(HttpRequestMessage request)
    {
        var host = (request.RequestUri?.Host ?? "").Trim();
        return string.IsNullOrWhiteSpace(host) ? "none" : host;
    }
}

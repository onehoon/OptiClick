using System.Net.Http;
using System.Security.Cryptography;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;

namespace OptiClick.Wpf.Shell.RemoteV2;

public sealed record ExtraBundleResourcePath
{
    public string Bundle { get; init; } = "";
    public string Version { get; init; } = "";
    public string Filename { get; init; } = "";
}

public sealed class AuthV2ArchiveDownloadRequestPreparer : IArchiveDownloadRequestPreparer
{
    private readonly IAuthV2CredentialStore _credentialStore;
    private readonly IOptiClickApiSession _session;
    private readonly Uri? _archiveResourceEndpoint;
    private readonly IAppLogger _logger;

    public AuthV2ArchiveDownloadRequestPreparer(
        IAuthV2CredentialStore credentialStore,
        IOptiClickApiSession session,
        Uri? archiveResourceEndpoint = null,
        IAppLogger? logger = null)
    {
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _archiveResourceEndpoint = archiveResourceEndpoint;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public Task PrepareAsync(
        HttpRequestMessage request,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!CanSignRequestForSource(sourceUrl))
        {
            _logger.Debug("Security", $"auth v2 archive prepare skipped reason=source_not_supported source_host={ResolveHostForLog(sourceUrl)}");
            return Task.CompletedTask;
        }

        var credential = _credentialStore.Load();
        if (credential?.IsUsable != true)
        {
            _logger.Warning("Security", "auth v2 archive prepare skipped reason=credential_missing");
            return Task.CompletedTask;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = CreateNonce();
        var signature = AuthV2RequestSigner.CreateSignature(
            request.Method?.Method ?? "GET",
            request.RequestUri?.AbsolutePath ?? "/",
            credential.ClientId,
            _session.AppStartId,
            timestamp,
            nonce,
            rawBody: "",
            credential.ClientSecret);

        SetHeader(request, "X-OptiClick-Client-Id", credential.ClientId);
        SetHeader(request, "X-OptiClick-App-Start-Id", _session.AppStartId);
        SetHeader(request, "X-OptiClick-Timestamp", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetHeader(request, "X-OptiClick-Nonce", nonce);
        SetHeader(request, "X-OptiClick-Signature", signature);
        _logger.Debug("Security", $"auth v2 archive prepare complete source_host={ResolveHostForLog(sourceUrl)}");
        return Task.CompletedTask;
    }

    private bool CanSignRequestForSource(string sourceUrl)
    {
        return _archiveResourceEndpoint is not null
            && TryParseExtraBundleResourceUrl(sourceUrl, out _)
            && HasSameOrigin(sourceUrl, _archiveResourceEndpoint);
    }

    public static bool TryParseExtraBundleResourceUrl(string sourceUrl, out ExtraBundleResourcePath resource)
    {
        resource = new ExtraBundleResourcePath();
        if (!Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (segments.Length != 6
            || !string.Equals(segments[0], "v2", StringComparison.Ordinal)
            || !string.Equals(segments[1], "resources", StringComparison.Ordinal)
            || !string.Equals(segments[2], "extra_bundle", StringComparison.Ordinal))
        {
            return false;
        }

        var bundle = segments[3].Trim();
        var version = segments[4].Trim();
        var filename = segments[5].Trim();
        if (!IsSafeExtraBundleToken(bundle)
            || !IsSafeExtraBundleToken(version)
            || !IsSafeExtraBundleToken(filename))
        {
            return false;
        }

        resource = new ExtraBundleResourcePath
        {
            Bundle = bundle,
            Version = version,
            Filename = filename
        };
        return true;
    }

    private static bool IsSafeExtraBundleToken(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal)
            || normalized.Contains('/', StringComparison.Ordinal)
            || normalized.Contains('\0', StringComparison.Ordinal))
        {
            return false;
        }

        return normalized.All(static ch =>
            ch is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.'
                or '_'
                or '-');
    }

    private static bool HasSameOrigin(string sourceUrl, Uri endpoint)
    {
        if (!Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var source))
        {
            return false;
        }

        return string.Equals(source.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(source.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
            && source.Port == endpoint.Port;
    }

    private static string CreateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(18);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static void SetHeader(HttpRequestMessage request, string name, string value)
    {
        if (request.Headers.Contains(name))
        {
            request.Headers.Remove(name);
        }

        request.Headers.TryAddWithoutValidation(name, value ?? "");
    }

    private static string ResolveHostForLog(string sourceUrl)
    {
        return Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var uri)
            ? uri.Host
            : "invalid";
    }
}

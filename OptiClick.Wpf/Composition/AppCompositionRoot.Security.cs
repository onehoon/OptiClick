using System.IO;
using System.Net.Http;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Composition;

public sealed record AppSecurityServices
{
    public required IOptiClickServerClock ServerClock { get; init; }
    public required IOptiClickApiRequestAuthenticator ApiRequestAuthenticator { get; init; }
    public required IOptiClickApiTicketStore TicketStore { get; init; }
    public required IArchiveDownloadRequestPreparer ArchiveDownloadRequestPreparer { get; init; }
}

public sealed partial class AppCompositionRoot
{
    public AppSecurityServices CreateAppSecurityServices(
        RemoteDataOptions remoteDataOptions,
        IAppLocalDataPathProvider localDataPathProvider,
        IAppLogger logger,
        IAppVersionProvider appVersionProvider,
        HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(localDataPathProvider);
        ArgumentNullException.ThrowIfNull(appVersionProvider);

        var sharedHttpClient = httpClient ?? new HttpClient();
        var effectiveLogger = logger ?? NullAppLogger.Instance;
        Func<string?> readAppVersion = () => appVersionProvider.GetCurrentVersion();
        var serverClock = new OptiClickServerClock(
            EnumerateTrustedWorkerApiEndpoints(remoteDataOptions),
            logger: effectiveLogger);

        var credentialStore = new ProtectedDataOptiClickClientCredentialStore(
            credentialPath: BuildCredentialPath(localDataPathProvider),
            logger: effectiveLogger);
        var registrationClient = new OptiClickClientRegistrationClient(
            sharedHttpClient,
            BuildApiEndpoint(remoteDataOptions, "/v1/client/register"),
            readAppVersion,
            effectiveLogger,
            serverClock: serverClock);
        var credentialProvider = new OptiClickClientCredentialProvider(
            credentialStore,
            registrationClient,
            readAppVersion,
            effectiveLogger);
        var ticketStore = new OptiClickApiTicketStore();
        var authenticator = new OptiClickApiRequestAuthenticator(
            credentialProvider,
            new OptiClickHmacSigner(),
            new OptiClickRequestCanonicalizer(),
            new OptiClickApiSession(),
            utcNow: () => serverClock.UtcNow,
            logger: effectiveLogger);
        var archiveRequestPreparer = new OptiClickArchiveDownloadRequestPreparer(
            authenticator,
            readAppVersion,
            BuildApiEndpoint(remoteDataOptions, "/v1/resources/extra_bundle"),
            effectiveLogger);

        return new AppSecurityServices
        {
            ServerClock = serverClock,
            ApiRequestAuthenticator = authenticator,
            TicketStore = ticketStore,
            ArchiveDownloadRequestPreparer = archiveRequestPreparer
        };
    }

    private static Uri? BuildApiEndpoint(RemoteDataOptions? remoteDataOptions, string path)
    {
        var normalizedPath = (path ?? "").Trim();
        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            normalizedPath = "/" + normalizedPath;
        }

        foreach (var endpoint in EnumerateCandidateEndpoints(remoteDataOptions))
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                || !IsWorkerApiEndpointCandidate(uri))
            {
                continue;
            }

            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedPath).Uri;
        }

        return null;
    }

    private static IEnumerable<Uri> EnumerateTrustedWorkerApiEndpoints(RemoteDataOptions? remoteDataOptions)
    {
        foreach (var endpoint in EnumerateCandidateEndpoints(remoteDataOptions))
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                && IsWorkerApiEndpointCandidate(uri))
            {
                yield return uri;
            }
        }
    }

    private static string BuildCredentialPath(IAppLocalDataPathProvider localDataPathProvider)
    {
        return Path.Combine(
            localDataPathProvider.RootDirectory,
            "Security",
            "client-credential.json");
    }

    private static IEnumerable<string> EnumerateCandidateEndpoints(RemoteDataOptions? remoteDataOptions)
    {
        if (remoteDataOptions is null)
        {
            yield break;
        }

        yield return remoteDataOptions.GpuBundleUrl;
        yield return remoteDataOptions.GpuBundleManifestUrl;
        yield return remoteDataOptions.RuntimeDataUrl;
        yield return remoteDataOptions.ManifestEndpoint;
    }

    private static bool IsWorkerApiEndpointCandidate(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host.Trim().ToLowerInvariant();
        if (host == "github.com"
            || host == "raw.githubusercontent.com"
            || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        return string.Equals(path, "/v1", StringComparison.Ordinal)
            || path.StartsWith("/v1/", StringComparison.Ordinal);
    }
}

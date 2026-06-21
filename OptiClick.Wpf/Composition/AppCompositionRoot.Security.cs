using System.Net.Http;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Composition;

public sealed record AppSecurityServices
{
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

        var credentialStore = new ProtectedDataOptiClickClientCredentialStore(logger: effectiveLogger);
        var registrationClient = new OptiClickClientRegistrationClient(
            sharedHttpClient,
            BuildApiEndpoint(remoteDataOptions, "/v1/client/register"),
            readAppVersion,
            effectiveLogger);
        var credentialProvider = new OptiClickClientCredentialProvider(
            credentialStore,
            registrationClient,
            readAppVersion);
        var ticketStore = new OptiClickApiTicketStore();
        var authenticator = new OptiClickApiRequestAuthenticator(
            credentialProvider,
            new OptiClickHmacSigner(),
            new OptiClickRequestCanonicalizer(),
            new OptiClickApiSession());
        var downloadTicketClient = new RemoteDownloadTicketClient(
            sharedHttpClient,
            BuildApiEndpoint(remoteDataOptions, "/v1/download-ticket"),
            authenticator,
            readAppVersion,
            effectiveLogger);
        var archiveRequestPreparer = new OptiClickArchiveDownloadRequestPreparer(
            downloadTicketClient,
            authenticator,
            readAppVersion);

        return new AppSecurityServices
        {
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
                || string.IsNullOrWhiteSpace(uri.Host))
            {
                continue;
            }

            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, normalizedPath).Uri;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateEndpoints(RemoteDataOptions? remoteDataOptions)
    {
        if (remoteDataOptions is null)
        {
            yield break;
        }

        yield return remoteDataOptions.RuntimeDataUrl;
        yield return remoteDataOptions.GpuBundleManifestUrl;
        yield return remoteDataOptions.GpuBundleUrl;
        yield return remoteDataOptions.ManifestEndpoint;
    }
}

using System.Net.Http;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;
using OptiClick.Infrastructure.Storage;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.RemoteV2;

namespace OptiClick.Wpf.Composition;

public sealed record AppSecurityServices
{
    public required IOptiClickServerClock ServerClock { get; init; }
    public required IOptiClickApiSession ApiSession { get; init; }
    public IOptiClickApiRequestAuthenticator? ApiRequestAuthenticator { get; init; }
    public required IOptiClickApiTicketStore TicketStore { get; init; }
    public IArchiveDownloadRequestPreparer? ArchiveDownloadRequestPreparer { get; init; }
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

        var effectiveLogger = logger ?? NullAppLogger.Instance;
        var serverClock = new OptiClickServerClock(
            EnumerateTrustedWorkerApiEndpoints(remoteDataOptions),
            logger: effectiveLogger);
        var apiSession = new OptiClickApiSession();
        var archiveRequestPreparer = new AuthV2ArchiveDownloadRequestPreparer(
            new AuthV2CredentialStore(localDataPathProvider, effectiveLogger),
            apiSession,
            serverClock,
            BuildDataV2ArchiveEndpoint(remoteDataOptions),
            effectiveLogger);

        return new AppSecurityServices
        {
            ServerClock = serverClock,
            ApiSession = apiSession,
            TicketStore = new OptiClickApiTicketStore(),
            ArchiveDownloadRequestPreparer = archiveRequestPreparer
        };
    }

    private static Uri? BuildDataV2ArchiveEndpoint(RemoteDataOptions? remoteDataOptions)
    {
        var baseUrl = (remoteDataOptions?.DataV2BaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate($"{baseUrl}/v2/resources/extra_bundle", UriKind.Absolute, out var endpoint))
        {
            return null;
        }

        return endpoint;
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

    private static IEnumerable<string> EnumerateCandidateEndpoints(RemoteDataOptions? remoteDataOptions)
    {
        if (remoteDataOptions is null)
        {
            yield break;
        }

        yield return BuildAuthV2SessionEndpoint(remoteDataOptions)?.AbsoluteUri ?? "";
        yield return BuildDataV2ArchiveEndpoint(remoteDataOptions)?.AbsoluteUri ?? "";
    }

    private static Uri? BuildAuthV2SessionEndpoint(RemoteDataOptions? remoteDataOptions)
    {
        var baseUrl = (remoteDataOptions?.AuthV2BaseUrl ?? "").Trim().TrimEnd('/');
        return !string.IsNullOrWhiteSpace(baseUrl)
               && Uri.TryCreate($"{baseUrl}/v2/auth/session/start", UriKind.Absolute, out var endpoint)
            ? endpoint
            : null;
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
        return string.Equals(path, "/v2/auth/session/start", StringComparison.Ordinal)
            || string.Equals(path, "/v2/resources/extra_bundle", StringComparison.Ordinal);
    }
}

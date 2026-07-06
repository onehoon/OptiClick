using System.Net.Http;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Logging;
using OptiClick.Infrastructure.Security;
using OptiClick.Wpf.Services;

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

        return new AppSecurityServices
        {
            ServerClock = serverClock,
            ApiSession = new OptiClickApiSession(),
            TicketStore = new OptiClickApiTicketStore()
        };
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

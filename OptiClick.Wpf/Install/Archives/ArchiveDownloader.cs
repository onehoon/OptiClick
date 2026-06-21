using System.Net.Http;
using OptiClick.Infrastructure.Security;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveDownloadResult
{
    public static ArchiveDownloadResult Success(
        string destinationPath,
        string verificationSource = "",
        string expectedSha256 = "",
        string actualSha256 = "")
    {
        return new ArchiveDownloadResult
        {
            IsSuccess = true,
            DestinationPath = destinationPath,
            VerificationSource = verificationSource,
            ExpectedSha256 = expectedSha256,
            ActualSha256 = actualSha256
        };
    }

    public static ArchiveDownloadResult Failure(string errorCode, string errorMessage = "")
    {
        return new ArchiveDownloadResult
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public bool IsSuccess { get; init; }
    public string DestinationPath { get; init; } = "";
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public string VerificationSource { get; init; } = "";
    public string ExpectedSha256 { get; init; } = "";
    public string ActualSha256 { get; init; } = "";

    public static ArchiveDownloadResult FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveDownloadResult result)
    {
        return new ArchiveDownloadResult
        {
            IsSuccess = result.IsSuccess,
            DestinationPath = result.DestinationPath,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            VerificationSource = result.VerificationSource,
            ExpectedSha256 = result.ExpectedSha256,
            ActualSha256 = result.ActualSha256
        };
    }
}

public interface IArchiveDownloader
{
    Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "");
}

public sealed class ArchiveDownloader : IArchiveDownloader
{
    private readonly OptiClick.Infrastructure.Archives.ArchiveDownloader _inner;

    public ArchiveDownloader(
        HttpClient httpClient,
        IArchiveDownloadRequestPreparer? requestPreparer = null)
    {
        _inner = new OptiClick.Infrastructure.Archives.ArchiveDownloader(
            httpClient ?? throw new ArgumentNullException(nameof(httpClient)),
            requestPreparer: requestPreparer);
    }

    internal ArchiveDownloader(OptiClick.Infrastructure.Archives.ArchiveDownloader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "")
    {
        // Compatibility wrapper: WPF contract stays stable while Infrastructure owns network/download implementation.
        var result = await _inner.DownloadAsync(url, destinationPath, timeout, cancellationToken, fallbackSha256);
        return ArchiveDownloadResult.FromInfrastructure(result);
    }
}

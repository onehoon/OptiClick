using System.Net.Http;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveDownloadResult
{
    public static ArchiveDownloadResult Success(string destinationPath)
    {
        return new ArchiveDownloadResult
        {
            IsSuccess = true,
            DestinationPath = destinationPath
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

    public static ArchiveDownloadResult FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveDownloadResult result)
    {
        return new ArchiveDownloadResult
        {
            IsSuccess = result.IsSuccess,
            DestinationPath = result.DestinationPath,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public interface IArchiveDownloader
{
    Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class ArchiveDownloader : IArchiveDownloader
{
    private readonly OptiClick.Infrastructure.Archives.ArchiveDownloader _inner;

    public ArchiveDownloader(HttpClient httpClient)
    {
        _inner = new OptiClick.Infrastructure.Archives.ArchiveDownloader(httpClient ?? throw new ArgumentNullException(nameof(httpClient)));
    }

    internal ArchiveDownloader(OptiClick.Infrastructure.Archives.ArchiveDownloader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        // Compatibility wrapper: WPF contract stays stable while Infrastructure owns network/download implementation.
        var result = await _inner.DownloadAsync(url, destinationPath, timeout, cancellationToken);
        return ArchiveDownloadResult.FromInfrastructure(result);
    }
}

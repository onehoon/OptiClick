using System.IO;
using System.Net.Http;

namespace OptiClick.Infrastructure.Archives;

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
}

public sealed class ArchiveDownloader
{
    private readonly HttpClient _httpClient;

    public ArchiveDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ArchiveDownloadResult.Failure("invalid_url");
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return ArchiveDownloadResult.Failure("invalid_destination");
        }

        var destination = Path.GetFullPath(destinationPath);
        var tempPath = destination + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(cts.Token);
            await using var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(file, cts.Token);
            file.Close();

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(tempPath, destination);
            return ArchiveDownloadResult.Success(destination);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryDelete(tempPath);
            return ArchiveDownloadResult.Failure("timeout");
        }
        catch (OperationCanceledException)
        {
            TryDelete(tempPath);
            return ArchiveDownloadResult.Failure("canceled");
        }
        catch (HttpRequestException)
        {
            TryDelete(tempPath);
            return ArchiveDownloadResult.Failure("request_failed");
        }
        catch
        {
            TryDelete(tempPath);
            return ArchiveDownloadResult.Failure("download_failed");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }
}

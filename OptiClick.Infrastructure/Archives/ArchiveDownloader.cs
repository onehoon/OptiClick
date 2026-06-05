using System.IO;
using System.Net.Http;

namespace OptiClick.Infrastructure.Archives;

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
}

public sealed class ArchiveDownloader
{
    private const int VerificationAttemptLimit = 2;

    private readonly HttpClient _httpClient;
    private readonly IArchiveFileVerifier _fileVerifier;

    public ArchiveDownloader(HttpClient httpClient, IArchiveFileVerifier? fileVerifier = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _fileVerifier = fileVerifier ?? new ArchiveFileVerifier(_httpClient);
    }

    public async Task<ArchiveDownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string fallbackSha256 = "")
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

        for (var attempt = 1; attempt <= VerificationAttemptLimit; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                await using var source = await response.Content.ReadAsStreamAsync(cts.Token);
                await using var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(file, cts.Token);
                file.Close();

                var verification = await _fileVerifier.VerifyArchiveFileAsync(tempPath, url, fallbackSha256, cts.Token);
                if (!verification.IsSuccess)
                {
                    TryDelete(tempPath);
                    if (ShouldRetryVerificationFailure(verification, attempt))
                    {
                        continue;
                    }

                    return ArchiveDownloadResult.Failure(
                        verification.ErrorCode,
                        BuildVerificationFailureMessage(verification, attempt));
                }

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(tempPath, destination);
                return ArchiveDownloadResult.Success(
                    destination,
                    verification.Source,
                    verification.ExpectedSha256,
                    verification.ActualSha256);
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

        return ArchiveDownloadResult.Failure("download_failed");
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

    private static bool ShouldRetryVerificationFailure(ArchiveFileVerificationResult verification, int attempt)
    {
        return attempt < VerificationAttemptLimit
               && string.Equals(verification.ErrorCode, "sha256_mismatch", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildVerificationFailureMessage(ArchiveFileVerificationResult verification, int attempt)
    {
        var parts = new List<string>
        {
            $"archive_verification_failed:{verification.ErrorCode}"
        };

        parts.Add($"attempt={attempt}");

        if (!string.IsNullOrWhiteSpace(verification.Source))
        {
            parts.Add($"source={verification.Source}");
        }

        if (!string.IsNullOrWhiteSpace(verification.ExpectedSha256))
        {
            parts.Add($"expected_sha256={verification.ExpectedSha256}");
        }

        if (!string.IsNullOrWhiteSpace(verification.ActualSha256))
        {
            parts.Add($"actual_sha256={verification.ActualSha256}");
        }

        return string.Join("; ", parts);
    }
}

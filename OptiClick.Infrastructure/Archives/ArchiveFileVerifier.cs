using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace OptiClick.Infrastructure.Archives;

public sealed record GitHubReleaseAssetIdentity
{
    public string Owner { get; init; } = "";
    public string Repository { get; init; } = "";
    public string Tag { get; init; } = "";
    public string AssetName { get; init; } = "";
}

public sealed record ArchiveFileVerificationResult
{
    public static ArchiveFileVerificationResult Success(string source = "", string expectedSha256 = "", string actualSha256 = "")
    {
        return new ArchiveFileVerificationResult
        {
            IsSuccess = true,
            Source = source,
            ExpectedSha256 = expectedSha256,
            ActualSha256 = actualSha256
        };
    }

    public static ArchiveFileVerificationResult Failure(string errorCode, string source = "", string expectedSha256 = "", string actualSha256 = "")
    {
        return new ArchiveFileVerificationResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            Source = source,
            ExpectedSha256 = expectedSha256,
            ActualSha256 = actualSha256
        };
    }

    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Source { get; init; } = "";
    public string ExpectedSha256 { get; init; } = "";
    public string ActualSha256 { get; init; } = "";
}

public interface IArchiveFileVerifier
{
    Task<ArchiveFileVerificationResult> VerifyArchiveFileAsync(
        string filePath,
        string downloadUrl,
        string fallbackSha256 = "",
        CancellationToken cancellationToken = default);
}

public sealed class ArchiveFileVerifier : IArchiveFileVerifier
{
    private const string GitHubApiAccept = "application/vnd.github+json";
    private const string UserAgent = "OptiClick";

    private readonly HttpClient _httpClient;

    public ArchiveFileVerifier(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ArchiveFileVerificationResult> VerifyArchiveFileAsync(
        string filePath,
        string downloadUrl,
        string fallbackSha256 = "",
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = (filePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath) || !File.Exists(normalizedPath))
        {
            return ArchiveFileVerificationResult.Failure("file_missing");
        }

        var fallback = NormalizeSha256(fallbackSha256);

        if (TryParseGitHubReleaseAssetUrl(downloadUrl, out var identity))
        {
            var digest = await TryGetGitHubAssetSha256DigestAsync(identity, cancellationToken);
            if (!string.IsNullOrWhiteSpace(digest))
            {
                return VerifyAgainstExpectedSha256(normalizedPath, digest, "github_release_digest");
            }

            return string.IsNullOrWhiteSpace(fallback)
                ? ArchiveFileVerificationResult.Success("github_digest_unavailable")
                : VerifyAgainstExpectedSha256(normalizedPath, fallback, "fallback_sha256");
        }

        return string.IsNullOrWhiteSpace(fallback)
            ? ArchiveFileVerificationResult.Success("not_configured")
            : VerifyAgainstExpectedSha256(normalizedPath, fallback, "fallback_sha256");
    }

    public static bool TryParseGitHubReleaseAssetUrl(
        string downloadUrl,
        out string owner,
        out string repo,
        out string tag,
        out string assetName)
    {
        owner = "";
        repo = "";
        tag = "";
        assetName = "";

        if (!TryParseGitHubReleaseAssetUrl(downloadUrl, out var identity))
        {
            return false;
        }

        owner = identity.Owner;
        repo = identity.Repository;
        tag = identity.Tag;
        assetName = identity.AssetName;
        return true;
    }

    public static bool TryParseGitHubReleaseAssetUrl(string downloadUrl, out GitHubReleaseAssetIdentity identity)
    {
        identity = new GitHubReleaseAssetIdentity();
        if (!Uri.TryCreate((downloadUrl ?? "").Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        if (segments.Length < 6
            || !string.Equals(segments[2], "releases", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(segments[3], "download", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var owner = segments[0].Trim();
        var repo = segments[1].Trim();
        var tag = segments[4].Trim();
        var assetName = string.Join("/", segments.Skip(5)).Trim();
        if (string.IsNullOrWhiteSpace(owner)
            || string.IsNullOrWhiteSpace(repo)
            || string.IsNullOrWhiteSpace(tag)
            || string.IsNullOrWhiteSpace(assetName))
        {
            return false;
        }

        identity = new GitHubReleaseAssetIdentity
        {
            Owner = owner,
            Repository = repo,
            Tag = tag,
            AssetName = assetName
        };
        return true;
    }

    public async Task<string?> TryGetGitHubAssetSha256DigestAsync(
        string owner,
        string repo,
        string tag,
        string assetName,
        CancellationToken cancellationToken = default)
    {
        return await TryGetGitHubAssetSha256DigestAsync(
            new GitHubReleaseAssetIdentity
            {
                Owner = owner,
                Repository = repo,
                Tag = tag,
                AssetName = assetName
            },
            cancellationToken);
    }

    private async Task<string?> TryGetGitHubAssetSha256DigestAsync(
        GitHubReleaseAssetIdentity identity,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiUrl =
                $"https://api.github.com/repos/{Uri.EscapeDataString(identity.Owner)}/{Uri.EscapeDataString(identity.Repository)}/releases/tags/{Uri.EscapeDataString(identity.Tag)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(GitHubApiAccept));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("assets", out var assets)
                || assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var asset in assets.EnumerateArray())
            {
                var name = ReadString(asset, "name");
                if (!string.Equals(name, identity.AssetName, StringComparison.Ordinal))
                {
                    continue;
                }

                var digest = NormalizeSha256(ReadString(asset, "digest"));
                return string.IsNullOrWhiteSpace(digest) ? null : digest;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public static string ComputeFileSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static ArchiveFileVerificationResult VerifyAgainstExpectedSha256(
        string filePath,
        string expectedSha256,
        string source)
    {
        var normalizedExpected = NormalizeSha256(expectedSha256);
        if (string.IsNullOrWhiteSpace(normalizedExpected))
        {
            return ArchiveFileVerificationResult.Failure("sha256_invalid", source);
        }

        try
        {
            var actual = ComputeFileSha256(filePath);
            return string.Equals(actual, normalizedExpected, StringComparison.OrdinalIgnoreCase)
                ? ArchiveFileVerificationResult.Success(source, normalizedExpected, actual)
                : ArchiveFileVerificationResult.Failure("sha256_mismatch", source, normalizedExpected, actual);
        }
        catch
        {
            return ArchiveFileVerificationResult.Failure("sha256_calculation_failed", source, normalizedExpected);
        }
    }

    private static string NormalizeSha256(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        const string Prefix = "sha256:";
        if (normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[Prefix.Length..].Trim();
        }

        normalized = normalized.ToLowerInvariant();
        return normalized.Length == 64 && normalized.All(IsHex)
            ? normalized
            : "";
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return "";
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? ""
            : value.ToString().Trim();
    }

    private static bool IsHex(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f';
    }
}

using System.IO;
using System.Net.Http;
using System.Text.Json;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public sealed record PublicAppUpdateLookupResult
{
    public bool IsSuccess { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public string ErrorCode { get; init; } = "";
    public AppUpdateInfo? UpdateInfo { get; init; }

    public static PublicAppUpdateLookupResult Failure(string errorCode)
    {
        return new PublicAppUpdateLookupResult
        {
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public interface IPublicAppUpdateService
{
    Task<PublicAppUpdateLookupResult> TryResolveUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default);
}

public sealed class PublicAppUpdateService : IPublicAppUpdateService
{
    private readonly string _dataV2BaseUrl;
    private readonly HttpClient _httpClient;
    private readonly IAppUpdateVersionComparer _versionComparer;
    private readonly IAppLogger _logger;

    public PublicAppUpdateService(
        string dataV2BaseUrl,
        HttpClient httpClient,
        IAppUpdateVersionComparer versionComparer,
        IAppLogger? logger = null)
    {
        _dataV2BaseUrl = (dataV2BaseUrl ?? "").Trim().TrimEnd('/');
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _versionComparer = versionComparer ?? throw new ArgumentNullException(nameof(versionComparer));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<PublicAppUpdateLookupResult> TryResolveUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_dataV2BaseUrl))
        {
            return PublicAppUpdateLookupResult.Failure("public_app_update_endpoint_missing");
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{_dataV2BaseUrl}/v2/public/app-update",
                cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return PublicAppUpdateLookupResult.Failure($"public_app_update_http_{(int)response.StatusCode}");
            }

            return Parse(content, currentVersion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return PublicAppUpdateLookupResult.Failure("public_app_update_canceled");
        }
        catch (Exception ex)
        {
            _logger.Warning("app-update", $"public app update request failed type={ex.GetType().Name}");
            return PublicAppUpdateLookupResult.Failure("public_app_update_request_failed");
        }
    }

    private PublicAppUpdateLookupResult Parse(string content, string currentVersion)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (!root.TryGetProperty("app_update", out var update) || update.ValueKind != JsonValueKind.Object)
            {
                return PublicAppUpdateLookupResult.Failure("public_app_update_missing");
            }

            var latestVersion = ReadString(update, "version");
            var downloadUrl = ReadString(update, "url");
            var filename = ReadString(update, "filename");
            if (string.IsNullOrWhiteSpace(filename) && Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
            {
                filename = Path.GetFileName(uri.LocalPath);
            }

            if (string.IsNullOrWhiteSpace(latestVersion)
                || string.IsNullOrWhiteSpace(downloadUrl)
                || string.IsNullOrWhiteSpace(filename))
            {
                return PublicAppUpdateLookupResult.Failure("public_app_update_invalid");
            }

            var packageType = ResolvePackageType(filename, downloadUrl);
            if (packageType == AppUpdatePackageType.Unknown)
            {
                return PublicAppUpdateLookupResult.Failure("public_app_update_package_unknown");
            }

            var sha256 = ReadString(update, "sha256");
            if (!IsSha256Hex(sha256))
            {
                return PublicAppUpdateLookupResult.Failure("public_app_update_sha256_invalid");
            }

            var isForced = ReadString(update, "force_update").Equals("true", StringComparison.OrdinalIgnoreCase);
            if (!_versionComparer.IsUpdateAvailable(currentVersion, latestVersion))
            {
                return new PublicAppUpdateLookupResult { IsSuccess = true, IsUpdateAvailable = false };
            }

            var displayVersion = ReadString(update, "display_version");
            if (string.IsNullOrWhiteSpace(displayVersion))
            {
                displayVersion = latestVersion;
            }

            return new PublicAppUpdateLookupResult
            {
                IsSuccess = true,
                IsUpdateAvailable = true,
                UpdateInfo = new AppUpdateInfo(
                    currentVersion,
                    latestVersion,
                    displayVersion,
                    downloadUrl,
                    filename,
                    ReadString(update, "note"),
                    packageType,
                    isForced,
                    sha256)
            };
        }
        catch (JsonException)
        {
            return PublicAppUpdateLookupResult.Failure("public_app_update_invalid_json");
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString()?.Trim() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => property.ToString(),
            _ => ""
        };
    }

    private static bool IsSha256Hex(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    private static AppUpdatePackageType ResolvePackageType(string filename, string url)
    {
        var extension = Path.GetExtension(filename);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(url);
        }

        return extension.ToLowerInvariant() switch
        {
            ".exe" => AppUpdatePackageType.Exe,
            ".zip" => AppUpdatePackageType.Zip,
            ".7z" => AppUpdatePackageType.SevenZip,
            _ => AppUpdatePackageType.Unknown
        };
    }
}

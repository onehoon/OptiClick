using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Services;

public static class CoverImageCacheService
{
    public const string DefaultCoverImageSource = "pack://application:,,,/Assets/Covers/default-cover.webp";
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(350);
    private static readonly HttpClient DefaultHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly ConcurrentDictionary<string, SharedCoverDownload> DownloadInProgress = new(StringComparer.OrdinalIgnoreCase);

    private sealed class SharedCoverDownload : IDisposable
    {
        public SharedCoverDownload(Func<CancellationToken, Task<string?>> createTask)
        {
            CancellationSource = new CancellationTokenSource();
            TaskSource = new Lazy<Task<string?>>(
                () => createTask(CancellationSource.Token),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public CancellationTokenSource CancellationSource { get; }
        public Lazy<Task<string?>> TaskSource { get; }

        public void Dispose()
        {
            CancellationSource.Dispose();
        }
    }

    public static string GetCacheDirectory()
    {
        return GetCacheDirectory(new AppLocalDataPathProvider());
    }

    public static string GetCacheDirectory(IAppLocalDataPathProvider localDataPathProvider)
    {
        return Path.Combine(
            (localDataPathProvider ?? new AppLocalDataPathProvider()).RootDirectory,
            "Covers");
    }

    public static string? TryGetCachedPath(string sourceUrl, string? steamAppId = null)
    {
        var normalizedUrl = (sourceUrl ?? "").Trim();
        if (!IsHttpUrl(normalizedUrl))
        {
            return null;
        }

        var steamCachePath = ResolveSteamAppCachePath(steamAppId);
        if (TryGetValidCachedPath(steamCachePath, deleteInvalid: false, out var cachedSteamPath))
        {
            return cachedSteamPath;
        }

        var inferredSteamAppId = TryExtractSteamAppIdFromUrl(normalizedUrl);
        if (!string.IsNullOrWhiteSpace(inferredSteamAppId))
        {
            var inferredSteamCachePath = ResolveSteamAppCachePath(inferredSteamAppId);
            if (TryGetValidCachedPath(inferredSteamCachePath, deleteInvalid: false, out var cachedInferredSteamPath))
            {
                return cachedInferredSteamPath;
            }
        }

        var urlCachePath = ResolveUrlCachePath(normalizedUrl);
        return TryGetValidCachedPath(urlCachePath, deleteInvalid: false, out var cachedUrlPath)
            ? cachedUrlPath
            : null;
    }

    public static bool IsCached(string sourceUrl, string? steamAppId = null)
    {
        return !string.IsNullOrWhiteSpace(TryGetCachedPath(sourceUrl, steamAppId));
    }

    public static string BuildSteamCoverUrl(string steamAppId)
    {
        var normalizedSteamAppId = (steamAppId ?? "").Trim();
        return $"https://shared.steamstatic.com/store_item_assets/steam/apps/{normalizedSteamAppId}/library_600x900_2x.jpg";
    }

    public static string ResolveLocalCoverSourceOrDefault(
        string? coverUrl,
        string? steamAppId,
        string defaultCoverSource = DefaultCoverImageSource)
    {
        var normalizedCoverUrl = (coverUrl ?? "").Trim();
        var normalizedSteamAppId = (steamAppId ?? "").Trim();

        if (IsHttpUrl(normalizedCoverUrl))
        {
            var cached = TryGetCachedPath(normalizedCoverUrl, normalizedSteamAppId);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return new Uri(cached, UriKind.Absolute).AbsoluteUri;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedSteamAppId))
        {
            var steamUrl = BuildSteamCoverUrl(normalizedSteamAppId);
            var cached = TryGetCachedPath(steamUrl, normalizedSteamAppId);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return new Uri(cached, UriKind.Absolute).AbsoluteUri;
            }
        }

        return string.IsNullOrWhiteSpace(defaultCoverSource)
            ? DefaultCoverImageSource
            : defaultCoverSource;
    }

    public static async Task<string?> EnsureCachedAsync(
        string sourceUrl,
        string? steamAppId = null,
        CancellationToken cancellationToken = default)
    {
        return await EnsureCachedAsync(sourceUrl, steamAppId, cancellationToken, DefaultHttpClient).ConfigureAwait(false);
    }

    internal static async Task<string?> EnsureCachedAsync(
        string sourceUrl,
        string? steamAppId,
        CancellationToken cancellationToken,
        HttpClient httpClient)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedUrl = (sourceUrl ?? "").Trim();
        if (!IsHttpUrl(normalizedUrl))
        {
            return null;
        }

        var steamCachePath = ResolveSteamAppCachePath(steamAppId);
        var urlCachePath = ResolveUrlCachePath(normalizedUrl);

        if (TryGetValidCachedPath(steamCachePath, deleteInvalid: true, out var cachedSteamPath))
        {
            return cachedSteamPath;
        }

        if (TryGetValidCachedPath(urlCachePath, deleteInvalid: true, out var cachedUrlPath))
        {
            return cachedUrlPath;
        }

        var client = httpClient ?? DefaultHttpClient;
        var downloadKey = ResolveDownloadTaskKey(normalizedUrl, steamAppId);
        var downloadTaskSource = DownloadInProgress.GetOrAdd(
            downloadKey,
            _ => CreateDownloadTaskSource(downloadKey, normalizedUrl, steamAppId, steamCachePath, urlCachePath, client));

        return await downloadTaskSource.TaskSource.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static SharedCoverDownload CreateDownloadTaskSource(
        string downloadKey,
        string normalizedUrl,
        string? steamAppId,
        string? steamCachePath,
        string urlCachePath,
        HttpClient httpClient)
    {
        return new SharedCoverDownload(
            cancellationToken => CreateDownloadTask(
                downloadKey,
                normalizedUrl,
                steamAppId,
                steamCachePath,
                urlCachePath,
                cancellationToken,
                httpClient));
    }

    private static Task<string?> CreateDownloadTask(
        string downloadKey,
        string normalizedUrl,
        string? steamAppId,
        string? steamCachePath,
        string urlCachePath,
        CancellationToken cancellationToken,
        HttpClient httpClient)
    {
        var task = DownloadAndCacheAsync(normalizedUrl, steamAppId, steamCachePath, urlCachePath, cancellationToken, httpClient);
        _ = task.ContinueWith(
            static (_, state) =>
            {
                var (key, originalTask) = ((string Key, Task<string?> Task))state!;
                if (DownloadInProgress.TryGetValue(key, out var currentTaskSource)
                    && currentTaskSource.TaskSource.IsValueCreated
                    && ReferenceEquals(currentTaskSource.TaskSource.Value, originalTask)
                    && DownloadInProgress.TryRemove(new KeyValuePair<string, SharedCoverDownload>(key, currentTaskSource)))
                {
                    currentTaskSource.Dispose();
                }
            },
            (downloadKey, task),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return task;
    }

    private static async Task<string?> DownloadAndCacheAsync(
        string normalizedUrl,
        string? steamAppId,
        string? steamCachePath,
        string urlCachePath,
        CancellationToken cancellationToken,
        HttpClient httpClient)
    {
        try
        {
            Directory.CreateDirectory(GetCacheDirectory());

            var primaryBytes = await TryDownloadImageWithRetryAsync(normalizedUrl, cancellationToken, httpClient).ConfigureAwait(false);
            var primaryResult = await TryWriteCacheAsync(primaryBytes, steamCachePath, urlCachePath, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(primaryResult))
            {
                return primaryResult;
            }

            var resolvedSteamAppId = ResolveSteamAppIdForLookup(steamAppId, normalizedUrl);
            if (string.IsNullOrWhiteSpace(resolvedSteamAppId))
            {
                return null;
            }

            var fallbackUrl = await TryFetchSteamAssetCoverUrlAsync(resolvedSteamAppId, cancellationToken, httpClient).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(fallbackUrl)
                || string.Equals(fallbackUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var fallbackBytes = await TryDownloadImageWithRetryAsync(fallbackUrl, cancellationToken, httpClient).ConfigureAwait(false);
            return await TryWriteCacheAsync(fallbackBytes, steamCachePath, urlCachePath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Ignore cache download failure.
        }

        return null;
    }

    private static string ResolveUrlCachePath(string sourceUrl)
    {
        var uri = new Uri(sourceUrl, UriKind.Absolute);
        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 8)
        {
            extension = ".img";
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sourceUrl));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return Path.Combine(GetCacheDirectory(), $"{hash}{extension}");
    }

    private static string? ResolveSteamAppCachePath(string? steamAppId)
    {
        var normalizedAppId = (steamAppId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedAppId))
        {
            return null;
        }

        return Path.Combine(GetCacheDirectory(), $"{normalizedAppId}.jpg");
    }

    private static bool TryGetValidCachedPath(string? path, bool deleteInvalid, out string cachedPath)
    {
        cachedPath = "";
        var normalizedPath = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath) || !File.Exists(normalizedPath))
        {
            return false;
        }

        if (IsCachedImageFileValid(normalizedPath))
        {
            cachedPath = normalizedPath;
            return true;
        }

        if (deleteInvalid)
        {
            TryDeleteFile(normalizedPath);
        }

        return false;
    }

    private static async Task<string?> TryWriteCacheAsync(
        byte[]? bytes,
        string? steamCachePath,
        string urlCachePath,
        CancellationToken cancellationToken)
    {
        if (bytes is null || bytes.Length == 0 || !IsSupportedImagePayload(bytes))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(steamCachePath))
        {
            await File.WriteAllBytesAsync(steamCachePath, bytes, cancellationToken).ConfigureAwait(false);
            return steamCachePath;
        }

        await File.WriteAllBytesAsync(urlCachePath, bytes, cancellationToken).ConfigureAwait(false);
        return urlCachePath;
    }

    private static string ResolveDownloadTaskKey(string sourceUrl, string? steamAppId)
    {
        var normalizedSteamAppId = (steamAppId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedSteamAppId))
        {
            normalizedSteamAppId = TryExtractSteamAppIdFromUrl(sourceUrl);
        }

        return string.IsNullOrWhiteSpace(normalizedSteamAppId)
            ? $"url::{sourceUrl}"
            : $"steam::{normalizedSteamAppId}";
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    private static string TryExtractSteamAppIdFromUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return "";
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return "";
        }

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!segments[i].Equals("apps", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = (segments[i + 1] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var isAllDigits = true;
            for (var index = 0; index < candidate.Length; index++)
            {
                if (!char.IsDigit(candidate[index]))
                {
                    isAllDigits = false;
                    break;
                }
            }

            if (isAllDigits)
            {
                return candidate;
            }
        }

        return "";
    }

    private static string ResolveSteamAppIdForLookup(string? steamAppId, string sourceUrl)
    {
        var normalizedSteamAppId = (steamAppId ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalizedSteamAppId)
            ? TryExtractSteamAppIdFromUrl(sourceUrl)
            : normalizedSteamAppId;
    }

    private static async Task<byte[]?> TryDownloadImageWithRetryAsync(
        string url,
        CancellationToken cancellationToken,
        HttpClient httpClient)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode >= 500 && attempt < MaxRetries)
                    {
                        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return null;
                }

                if (!IsImageContentType(response))
                {
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                return bytes.Length == 0 || !IsSupportedImagePayload(bytes) ? null : bytes;
            }
            catch (HttpRequestException)
            {
                if (attempt >= MaxRetries)
                {
                    return null;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                if (attempt >= MaxRetries)
                {
                    return null;
                }
            }

            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<string> TryFetchSteamAssetCoverUrlAsync(
        string steamAppId,
        CancellationToken cancellationToken,
        HttpClient httpClient)
    {
        const string apiUrl = "https://api.steampowered.com/IStoreBrowseService/GetItems/v1/";
        if (!int.TryParse(steamAppId, out var appId))
        {
            return "";
        }

        try
        {
            var input = new
            {
                ids = new[] { new { appid = appId } },
                context = new { country_code = "US" },
                data_request = new { include_assets = true }
            };
            var inputJson = JsonSerializer.Serialize(input);
            var encodedInput = Uri.EscapeDataString(inputJson);
            var requestUri = $"{apiUrl}?input_json={encodedInput}";

            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return "";
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return "";
            }

            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("response", out var responseElement)
                || !responseElement.TryGetProperty("store_items", out var itemsElement)
                || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return "";
            }

            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!item.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var value = ReadAssetString(assetsElement, "library_capsule_2x");
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = ReadAssetString(assetsElement, "library_capsule");
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }

                return $"https://shared.steamstatic.com/store_item_assets/steam/apps/{steamAppId}/{value.TrimStart('/')}";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Ignore steam asset lookup failure.
        }

        return "";
    }

    private static bool IsImageContentType(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        return string.IsNullOrWhiteSpace(mediaType)
               || mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCachedImageFileValid(string path)
    {
        try
        {
            Span<byte> header = stackalloc byte[16];
            using var stream = File.OpenRead(path);
            var read = stream.Read(header);
            if (!HasSupportedImageSignature(header[..read]))
            {
                return false;
            }

            stream.Position = 0;
            return CanDecodeImageStream(stream);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSupportedImagePayload(byte[] bytes)
    {
        return HasSupportedImageSignature(bytes) && CanDecodeImagePayload(bytes);
    }

    private static bool HasSupportedImageSignature(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return true;
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return true;
        }

        if (bytes.Length >= 6
            && bytes[0] == 0x47
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x38
            && (bytes[4] == 0x37 || bytes[4] == 0x39)
            && bytes[5] == 0x61)
        {
            return true;
        }

        if (bytes.Length >= 2
            && bytes[0] == 0x42
            && bytes[1] == 0x4D)
        {
            return true;
        }

        if (bytes.Length >= 12
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46
            && bytes[8] == 0x57
            && bytes[9] == 0x45
            && bytes[10] == 0x42
            && bytes[11] == 0x50)
        {
            return true;
        }

        if (bytes.Length >= 4
            && bytes[0] == 0x00
            && bytes[1] == 0x00
            && bytes[2] == 0x01
            && bytes[3] == 0x00)
        {
            return true;
        }

        return false;
    }

    private static bool CanDecodeImagePayload(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            return CanDecodeImageStream(stream);
        }
        catch
        {
            return false;
        }
    }

    private static bool CanDecodeImageStream(Stream stream)
    {
        try
        {
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            return decoder.Frames.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
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
            // Ignore stale cache cleanup failure.
        }
    }

    private static string ReadAssetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return "";
        }

        return property.GetString()?.Trim() ?? "";
    }
}

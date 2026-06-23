using System.IO;
using System.Text.Json;
using OptiClick.Core.Install;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Startup;

public interface ICoverCacheBootstrapService
{
    bool IsReady();

    Task<CoverCacheBootstrapResult> BootstrapAsync(
        IProgress<CoverCacheBootstrapState>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record CoverCacheBootstrapResult
{
    public static CoverCacheBootstrapResult NotRequired()
    {
        return new CoverCacheBootstrapResult
        {
            State = CoverCacheBootstrapState.NotRequired
        };
    }

    public static CoverCacheBootstrapResult Completed(int copiedFileCount = 0)
    {
        return new CoverCacheBootstrapResult
        {
            State = CoverCacheBootstrapState.Completed,
            Attempted = true,
            CopiedFileCount = copiedFileCount
        };
    }

    public static CoverCacheBootstrapResult FailedFallbackEnabled(string errorCode)
    {
        return new CoverCacheBootstrapResult
        {
            State = CoverCacheBootstrapState.FailedFallbackEnabled,
            Attempted = true,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "cover_cache_bootstrap_failed" : errorCode
        };
    }

    public CoverCacheBootstrapState State { get; init; } = CoverCacheBootstrapState.NotRequired;
    public bool Attempted { get; init; }
    public string ErrorCode { get; init; } = "";
    public int CopiedFileCount { get; init; }
}

public sealed class CoverCacheBootstrapService : ICoverCacheBootstrapService
{
    private const string CoverCacheBundleHost = "opticlick" + "-data-api" + "." + "onehoon" + ".workers" + ".dev";
    public const string CoverCacheBundleUrl = "https://" + CoverCacheBundleHost + "/v1/resources/extra_bundle/cover_bundle/0.1/cover_cache.zip";
    public const string CoverCacheBundleFileName = "cover_cache.zip";
    public const string CoverCacheManifestFileName = "cover_cache_manifest.json";

    private const string CoverCacheBundleAlias = "cover_cache";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IExtraBundleInstaller _extraBundleInstaller;
    private readonly IInstallFileSystem _fileSystem;
    private readonly IAppLocalDataPathProvider _pathProvider;
    private readonly IAppLogger _logger;

    public CoverCacheBootstrapService(
        IExtraBundleInstaller extraBundleInstaller,
        IInstallFileSystem fileSystem,
        IAppLocalDataPathProvider pathProvider,
        IAppLogger? logger = null)
    {
        _extraBundleInstaller = extraBundleInstaller ?? throw new ArgumentNullException(nameof(extraBundleInstaller));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<CoverCacheBootstrapResult> BootstrapAsync(
        IProgress<CoverCacheBootstrapState>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsReady())
        {
            progress?.Report(CoverCacheBootstrapState.NotRequired);
            return CoverCacheBootstrapResult.NotRequired();
        }

        progress?.Report(CoverCacheBootstrapState.Downloading);

        try
        {
            var coverCacheDirectory = CoverImageCacheService.GetCacheDirectory(_pathProvider);
            _fileSystem.CreateDirectory(coverCacheDirectory);
            Directory.CreateDirectory(_pathProvider.ManifestDirectory);

            var result = await _extraBundleInstaller.InstallAsync(
                BuildComponentInstallContext(coverCacheDirectory),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (result.Status != ComponentInstallStatus.Success)
            {
                var errorCode = string.IsNullOrWhiteSpace(result.ErrorCode)
                    ? "cover_cache_extra_bundle_failed"
                    : result.ErrorCode;
                _logger.Warning("Startup", $"cover_cache_bootstrap fallback_enabled reason={NormalizeForLog(errorCode)}");
                progress?.Report(CoverCacheBootstrapState.FailedFallbackEnabled);
                return CoverCacheBootstrapResult.FailedFallbackEnabled(errorCode);
            }

            progress?.Report(CoverCacheBootstrapState.Extracting);
            var copiedFileCount = ResolveCopiedFileCount(result.Message);
            WriteManifest(copiedFileCount);
            progress?.Report(CoverCacheBootstrapState.Completed);
            return CoverCacheBootstrapResult.Completed(copiedFileCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Warning("Startup", $"cover_cache_bootstrap fallback_enabled type={ex.GetType().Name}");
            progress?.Report(CoverCacheBootstrapState.FailedFallbackEnabled);
            return CoverCacheBootstrapResult.FailedFallbackEnabled("cover_cache_bootstrap_failed");
        }
    }

    public bool IsReady()
    {
        var manifestPath = GetManifestPath();
        if (!_fileSystem.FileExists(manifestPath))
        {
            return false;
        }

        CoverCacheManifest? manifest;
        try
        {
            var json = File.ReadAllText(manifestPath);
            manifest = JsonSerializer.Deserialize<CoverCacheManifest>(json, SerializerOptions);
        }
        catch
        {
            return false;
        }

        if (manifest is null || manifest.CopiedFileCount <= 0)
        {
            return false;
        }

        var coverCacheDirectory = CoverImageCacheService.GetCacheDirectory(_pathProvider);
        if (!_fileSystem.DirectoryExists(coverCacheDirectory))
        {
            return false;
        }

        try
        {
            return _fileSystem.EnumerateFiles(coverCacheDirectory, "*", SearchOption.AllDirectories)
                .Take(manifest.CopiedFileCount)
                .Count() >= manifest.CopiedFileCount;
        }
        catch
        {
            return false;
        }
    }

    private ComponentInstallContext BuildComponentInstallContext(string coverCacheDirectory)
    {
        return new ComponentInstallContext
        {
            TargetPath = coverCacheDirectory,
            ExecutionDescriptor = new InstallExecutionDescriptor
            {
                GameDescriptor = new InstallGameDescriptor
                {
                    ExtraBundle = CoverCacheBundleAlias
                }
            },
            ExtraBundleAlias = CoverCacheBundleAlias,
            ModuleDownloadLinks = ModuleDownloadLinkCatalog.FromRaw(
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [CoverCacheBundleAlias] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["url"] = CoverCacheBundleUrl,
                        ["filename"] = CoverCacheBundleFileName
                    }
                })
        };
    }

    private void WriteManifest(int copiedFileCount)
    {
        var manifest = new CoverCacheManifest
        {
            BundleUrl = CoverCacheBundleUrl,
            FileName = CoverCacheBundleFileName,
            CompletedAt = DateTimeOffset.UtcNow,
            CopiedFileCount = copiedFileCount
        };

        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        AtomicFileWriter.WriteAllTextAtomic(GetManifestPath(), json);
    }

    private string GetManifestPath()
    {
        return Path.Combine(_pathProvider.ManifestDirectory, CoverCacheManifestFileName);
    }

    private static int ResolveCopiedFileCount(string message)
    {
        var normalized = (message ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return 0;
        }

        const string key = "copied_files=";
        var index = normalized.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return 0;
        }

        var valueStart = index + key.Length;
        var valueEnd = normalized.IndexOf(';', valueStart);
        var value = valueEnd < 0
            ? normalized[valueStart..]
            : normalized[valueStart..valueEnd];
        return int.TryParse(value, out var count) ? count : 0;
    }

    private static string NormalizeForLog(string value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "unknown"
            : normalized.Replace(' ', '_');
    }

    private sealed record CoverCacheManifest
    {
        public string BundleUrl { get; init; } = "";
        public string FileName { get; init; } = "";
        public DateTimeOffset CompletedAt { get; init; }
        public int CopiedFileCount { get; init; }
    }
}

public sealed class NoOpCoverCacheBootstrapService : ICoverCacheBootstrapService
{
    public static NoOpCoverCacheBootstrapService Instance { get; } = new();

    private NoOpCoverCacheBootstrapService()
    {
    }

    public Task<CoverCacheBootstrapResult> BootstrapAsync(
        IProgress<CoverCacheBootstrapState>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(CoverCacheBootstrapState.NotRequired);
        return Task.FromResult(CoverCacheBootstrapResult.NotRequired());
    }

    public bool IsReady()
    {
        return true;
    }
}

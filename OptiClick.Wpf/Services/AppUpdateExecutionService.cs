using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Logging;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Services;

public sealed record AppUpdateExecutionResult
{
    public static AppUpdateExecutionResult Success(AppUpdatePreparedFile preparedFile)
    {
        return new AppUpdateExecutionResult
        {
            IsSuccess = true,
            PreparedFile = preparedFile
        };
    }

    public static AppUpdateExecutionResult Failure(string errorCode, string errorMessage = "")
    {
        return new AppUpdateExecutionResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public AppUpdatePreparedFile? PreparedFile { get; init; }
}

public interface IAppUpdateExecutionService
{
    Task<AppUpdateExecutionResult> PrepareAndCopyUpdateAsync(
        AppUpdateInfo updateInfo,
        CancellationToken cancellationToken = default);

    AppUpdateExecutionResult TryLaunchCopiedExecutable(string finalCopiedExePath);
}

public sealed class AppUpdateExecutionService : IAppUpdateExecutionService
{
    public const string LatestReleaseUrl = "https://github.com/onehoon/OptiClick/releases/latest";

    private static readonly TimeSpan DefaultDownloadTimeout = TimeSpan.FromMinutes(5);

    private readonly IAppLocalDataPathProvider _localDataPathProvider;
    private readonly IArchiveDownloader _archiveDownloader;
    private readonly IArchiveExtractor _archiveExtractor;
    private readonly IAppLogger _logger;
    private readonly Func<ProcessStartInfo, int?> _processStarter;
    private readonly Func<int, bool> _foregroundPermissionGrant;
    private readonly Func<string?> _processPathProvider;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _downloadTimeout;

    public AppUpdateExecutionService(
        IAppLocalDataPathProvider? localDataPathProvider = null,
        IArchiveDownloader? archiveDownloader = null,
        IArchiveExtractor? archiveExtractor = null,
        IAppLogger? logger = null,
        Func<ProcessStartInfo, int?>? processStarter = null,
        Func<int, bool>? foregroundPermissionGrant = null,
        Func<string?>? processPathProvider = null,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? downloadTimeout = null)
    {
        _localDataPathProvider = localDataPathProvider ?? new AppLocalDataPathProvider();
        _archiveDownloader = archiveDownloader ?? new ArchiveDownloader(new HttpClient());
        _archiveExtractor = archiveExtractor ?? new WindowsTarArchiveExtractor(
            new ArchiveProcessRunner(),
            new OverrideableOperatingSystemSupportPolicy(
                new Windows11OnlyOperatingSystemSupportPolicy()));
        _logger = logger ?? NullAppLogger.Instance;
        _processStarter = processStarter ?? StartProcess;
        _foregroundPermissionGrant = foregroundPermissionGrant ?? TryAllowSetForegroundWindow;
        _processPathProvider = processPathProvider ?? (() => Environment.ProcessPath);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _downloadTimeout = downloadTimeout ?? DefaultDownloadTimeout;
    }

    public async Task<AppUpdateExecutionResult> PrepareAndCopyUpdateAsync(
        AppUpdateInfo updateInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (updateInfo is null
                || string.IsNullOrWhiteSpace(updateInfo.DownloadUrl)
                || string.IsNullOrWhiteSpace(updateInfo.LatestVersion))
            {
                return AppUpdateExecutionResult.Failure("invalid_update_info");
            }

            var processPath = (_processPathProvider() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return AppUpdateExecutionResult.Failure("process_path_missing");
            }

            var currentExeDirectory = Path.GetDirectoryName(processPath);
            if (string.IsNullOrWhiteSpace(currentExeDirectory))
            {
                return AppUpdateExecutionResult.Failure("process_directory_missing");
            }

            if (!Directory.Exists(currentExeDirectory))
            {
                return AppUpdateExecutionResult.Failure("process_directory_not_found");
            }

            var workspaceRoot = BuildWorkspaceRoot(_localDataPathProvider, _clock());
            var downloadRoot = Path.Combine(workspaceRoot, "download");
            var extractRoot = Path.Combine(workspaceRoot, "extracted");
            Directory.CreateDirectory(downloadRoot);
            Directory.CreateDirectory(extractRoot);

            var fileName = ResolveDownloadFileName(updateInfo);
            var downloadTempPath = Path.Combine(downloadRoot, fileName + ".download");
            var downloadedOriginalPath = Path.Combine(downloadRoot, fileName);

            var download = await _archiveDownloader.DownloadAsync(
                updateInfo.DownloadUrl,
                downloadTempPath,
                _downloadTimeout,
                cancellationToken,
                updateInfo.Sha256);
            if (!download.IsSuccess)
            {
                _logger.Warning("app-update", $"update download failed code={download.ErrorCode}");
                return AppUpdateExecutionResult.Failure(
                    $"download_{NormalizeStatusCode(download.ErrorCode, "failed")}",
                    download.ErrorMessage);
            }

            if (!File.Exists(downloadTempPath))
            {
                _logger.Warning("app-update", "update download temp file missing");
                return AppUpdateExecutionResult.Failure("download_temp_missing");
            }

            var tempFileSize = new FileInfo(downloadTempPath).Length;
            if (tempFileSize <= 0)
            {
                _logger.Warning("app-update", "update download temp file is empty");
                return AppUpdateExecutionResult.Failure("download_temp_empty");
            }

            try
            {
                File.Move(downloadTempPath, downloadedOriginalPath, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.Warning("app-update", $"update download rename failed type={ex.GetType().Name}");
                return AppUpdateExecutionResult.Failure("download_rename_failed");
            }

            if (!File.Exists(downloadedOriginalPath))
            {
                _logger.Warning("app-update", "update download final file missing");
                return AppUpdateExecutionResult.Failure("download_final_missing");
            }

            if (new FileInfo(downloadedOriginalPath).Length <= 0)
            {
                _logger.Warning("app-update", "update download final file is empty");
                return AppUpdateExecutionResult.Failure("download_final_empty");
            }

            var hashValidationError = await ValidateSha256Async(updateInfo, downloadedOriginalPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(hashValidationError))
            {
                _logger.Warning("app-update", $"update sha256 validation failed code={hashValidationError}");
                return AppUpdateExecutionResult.Failure(hashValidationError);
            }

            var resolvedExePath = updateInfo.PackageType switch
            {
                AppUpdatePackageType.Exe => downloadedOriginalPath,
                AppUpdatePackageType.Zip or AppUpdatePackageType.SevenZip
                    => await ResolveExecutableFromArchiveAsync(downloadedOriginalPath, extractRoot, cancellationToken),
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(resolvedExePath) || !File.Exists(resolvedExePath))
            {
                _logger.Warning("app-update", "update executable resolution failed");
                return AppUpdateExecutionResult.Failure("resolve_exe_failed");
            }

            if (new FileInfo(resolvedExePath).Length <= 0)
            {
                _logger.Warning("app-update", "resolved update executable is empty");
                return AppUpdateExecutionResult.Failure("resolved_exe_empty");
            }

            var resolvedExeFileName = Path.GetFileName(resolvedExePath);
            var finalCopiedExePath = Path.Combine(currentExeDirectory, resolvedExeFileName);
            if (string.Equals(
                    Path.GetFullPath(finalCopiedExePath),
                    Path.GetFullPath(processPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning("app-update", "refusing to overwrite current running executable");
                return AppUpdateExecutionResult.Failure("refuse_overwrite_current_exe");
            }

            try
            {
                File.Copy(resolvedExePath, finalCopiedExePath, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.Warning("app-update", $"final update executable copy failed type={ex.GetType().Name}");
                return AppUpdateExecutionResult.Failure("copy_final_exe_failed");
            }

            if (!File.Exists(finalCopiedExePath))
            {
                _logger.Warning("app-update", "copied update executable missing");
                return AppUpdateExecutionResult.Failure("copied_exe_missing");
            }

            if (new FileInfo(finalCopiedExePath).Length <= 0)
            {
                _logger.Warning("app-update", "copied update executable is empty");
                return AppUpdateExecutionResult.Failure("copied_exe_empty");
            }

            return AppUpdateExecutionResult.Success(new AppUpdatePreparedFile(
                downloadedOriginalPath,
                resolvedExePath,
                resolvedExeFileName,
                finalCopiedExePath));
        }
        catch (OperationCanceledException)
        {
            return AppUpdateExecutionResult.Failure("canceled");
        }
        catch (Exception ex)
        {
            _logger.Error("app-update", "prepare/copy update failed", ex);
            return AppUpdateExecutionResult.Failure("prepare_copy_exception");
        }
    }

    public AppUpdateExecutionResult TryLaunchCopiedExecutable(string finalCopiedExePath)
    {
        var path = (finalCopiedExePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return AppUpdateExecutionResult.Failure("launch_path_missing");
        }

        if (!File.Exists(path))
        {
            return AppUpdateExecutionResult.Failure("launch_path_not_found");
        }

        if (new FileInfo(path).Length <= 0)
        {
            return AppUpdateExecutionResult.Failure("launch_path_empty");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path) ?? "",
            Arguments = AppUpdateStartupArguments.ForegroundAfterUpdate,
            UseShellExecute = true
        };
        var processId = _processStarter(startInfo);
        if (processId is null)
        {
            _logger.Warning("app-update", "update executable launch failed");
            return AppUpdateExecutionResult.Failure("launch_failed");
        }

        TryGrantForegroundPermission(processId.Value);
        return AppUpdateExecutionResult.Success(new AppUpdatePreparedFile("", "", Path.GetFileName(path), path));
    }

    private static string BuildWorkspaceRoot(IAppLocalDataPathProvider localDataPathProvider, DateTimeOffset utcNow)
    {
        return Path.Combine(
            localDataPathProvider.InstallExecutionTempDirectory,
            "AppUpdate",
            utcNow.ToString("yyyyMMddHHmmss"));
    }

    private static int? StartProcess(ProcessStartInfo info)
    {
        try
        {
            using var process = Process.Start(info);
            return process?.Id;
        }
        catch
        {
            return null;
        }
    }

    private void TryGrantForegroundPermission(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        try
        {
            _foregroundPermissionGrant(processId);
        }
        catch (Exception ex)
        {
            _logger.Warning("app-update", $"foreground permission grant failed type={ex.GetType().Name}");
        }
    }

    private static bool TryAllowSetForegroundWindow(int processId)
    {
        if (!OperatingSystem.IsWindows() || processId <= 0)
        {
            return false;
        }

        try
        {
            return AllowSetForegroundWindow(processId);
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private static string ResolveDownloadFileName(AppUpdateInfo updateInfo)
    {
        var candidate = (updateInfo.DownloadFileName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            if (Uri.TryCreate(updateInfo.DownloadUrl, UriKind.Absolute, out var uri))
            {
                candidate = Path.GetFileName(uri.LocalPath);
            }
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = updateInfo.PackageType switch
            {
                AppUpdatePackageType.Exe => "OptiClick_update.exe",
                AppUpdatePackageType.Zip => "OptiClick_update.zip",
                AppUpdatePackageType.SevenZip => "OptiClick_update.7z",
                _ => "OptiClick_update.bin"
            };
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            candidate = candidate.Replace(invalidChar, '_');
        }

        return candidate;
    }

    private static async Task<string> ValidateSha256Async(
        AppUpdateInfo updateInfo,
        string filePath,
        CancellationToken cancellationToken)
    {
        var expected = (updateInfo.Sha256 ?? "").Trim();
        if (string.IsNullOrWhiteSpace(expected))
        {
            return "";
        }

        if (!File.Exists(filePath))
        {
            return "sha256_file_missing";
        }

        await using var stream = File.OpenRead(filePath);
        var actualBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var actual = Convert.ToHexString(actualBytes);
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            ? ""
            : "sha256_mismatch";
    }

    private async Task<string> ResolveExecutableFromArchiveAsync(
        string archivePath,
        string extractRoot,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(extractRoot);

        var extraction = await _archiveExtractor.ExtractAsync(archivePath, extractRoot, cancellationToken);

        if (!extraction.IsSuccess)
        {
            _logger.Warning("app-update", $"update archive extract failed code={extraction.ErrorCode}");
            return "";
        }

        var primaryCandidates = Directory.GetFiles(extractRoot, "OptiClick_v*.exe", SearchOption.AllDirectories);
        if (primaryCandidates.Length == 1)
        {
            return primaryCandidates[0];
        }

        if (primaryCandidates.Length > 1)
        {
            _logger.Warning("app-update", "multiple OptiClick_v*.exe candidates were found");
            return "";
        }

        var secondaryCandidates = Directory.GetFiles(extractRoot, "OptiClick*.exe", SearchOption.AllDirectories);
        if (secondaryCandidates.Length == 1)
        {
            return secondaryCandidates[0];
        }

        if (secondaryCandidates.Length > 1)
        {
            _logger.Warning("app-update", "multiple OptiClick*.exe candidates were found");
        }
        else
        {
            _logger.Warning("app-update", "no OptiClick executable candidate was found");
        }

        return "";
    }

    private static string NormalizeStatusCode(string value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

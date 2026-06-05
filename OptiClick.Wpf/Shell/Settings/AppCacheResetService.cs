using System.IO;
using System.Linq;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Shell.Settings;

internal enum AppCacheResetTargetKind
{
    Directory,
    File
}

internal sealed record AppCacheResetTarget(string Path, AppCacheResetTargetKind Kind);

internal sealed class AppCacheResetService
{
    internal const string FirstRunStateFileName = "startup_state.json";

    private readonly IAppLocalDataPathProvider _localDataPathProvider;
    private readonly IAppLogger _appLogger;

    public AppCacheResetService(
        IAppLocalDataPathProvider localDataPathProvider,
        IAppLogger appLogger)
    {
        _localDataPathProvider = localDataPathProvider ?? throw new ArgumentNullException(nameof(localDataPathProvider));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public bool TryReset()
    {
        var targets = BuildTargets();
        foreach (var target in targets)
        {
            if (!IsSafeCacheSubPath(target.Path))
            {
                _appLogger.Warning("settings", $"reset_app_cache skipped reason=unsafe_path path={target.Path}");
                return false;
            }
        }

        foreach (var target in targets)
        {
            var deleted = target.Kind == AppCacheResetTargetKind.Directory
                ? TryDeleteDirectory(target.Path)
                : TryDeleteFile(target.Path);
            if (!deleted)
            {
                _appLogger.Warning("settings", $"reset_app_cache failed reason=delete_failed path={target.Path}");
                return false;
            }
        }

        _appLogger.Info("settings", "reset_app_cache completed");
        return true;
    }

    internal IReadOnlyList<AppCacheResetTarget> BuildTargets()
    {
        var archiveCachePaths = ArchiveCachePaths.CreateDefault(_localDataPathProvider);
        var manifestDirectory = _localDataPathProvider.ManifestDirectory;
        return
        [
            DirectoryTarget(archiveCachePaths.Root),
            DirectoryTarget(archiveCachePaths.ManifestRoot),
            DirectoryTarget(_localDataPathProvider.InstallExecutionTempDirectory),
            FileTarget(Path.Combine(manifestDirectory, FirstRunStateFileName))
        ];
    }

    private bool IsSafeCacheSubPath(string path)
    {
        var root = (_localDataPathProvider.RootDirectory ?? "").Trim();
        var target = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        try
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedTarget = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var prefix = normalizedRoot + Path.DirectorySeparatorChar;
            return normalizedTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDeleteDirectory(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return !File.Exists(directory) || TryDeleteFile(directory);
            }

            foreach (var filePath in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            foreach (var directoryPath in Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories)
                         .OrderByDescending(static path => path.Length))
            {
                File.SetAttributes(directoryPath, FileAttributes.Normal);
            }

            File.SetAttributes(directory, FileAttributes.Normal);
            Directory.Delete(directory, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDeleteFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AppCacheResetTarget DirectoryTarget(string path)
    {
        return new AppCacheResetTarget(path, AppCacheResetTargetKind.Directory);
    }

    private static AppCacheResetTarget FileTarget(string path)
    {
        return new AppCacheResetTarget(path, AppCacheResetTargetKind.File);
    }
}

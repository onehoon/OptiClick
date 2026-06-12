using System.IO;
using OptiClick.Core.Games;
using OptiClick.Core.Scan;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Scan;

public sealed class ExecutableScanService : IExecutableScanService
{
    private readonly IAppLogger _logger;

    public ExecutableScanService(IAppLogger? logger = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
    }

    public Task<ExecutableScanResult> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        return ScanCoreAsync(folderPath, allowedExeNames: null, filterByAllowedExeNames: false, cancellationToken);
    }

    public Task<ExecutableScanResult> ScanAsync(
        string folderPath,
        IReadOnlySet<string> allowedExeNames,
        CancellationToken cancellationToken = default)
    {
        if (allowedExeNames is null || allowedExeNames.Count == 0)
        {
            var normalizedFolderPath = (folderPath ?? "").Trim();
            return Task.FromResult(new ExecutableScanResult
            {
                FolderPath = normalizedFolderPath,
                Executables = [],
                ScannedExecutableCount = 0,
                SkippedDirectoryCount = 0
            });
        }

        return ScanCoreAsync(folderPath, allowedExeNames, filterByAllowedExeNames: true, cancellationToken);
    }

    private Task<ExecutableScanResult> ScanCoreAsync(
        string folderPath,
        IReadOnlySet<string>? allowedExeNames,
        bool filterByAllowedExeNames,
        CancellationToken cancellationToken)
    {
        var normalizedFolderPath = (folderPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedFolderPath) || !Directory.Exists(normalizedFolderPath))
        {
            _logger.Warning("scan", "skipped invalid_or_missing_folder");
            return Task.FromResult(new ExecutableScanResult
            {
                FolderPath = normalizedFolderPath,
                Executables = [],
                ScannedExecutableCount = 0,
                SkippedDirectoryCount = 0
            });
        }

        var executables = new List<DetectedExecutable>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(normalizedFolderPath);
        var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedDirectoryCount = 0;
        var scannedExecutableCount = 0;

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pendingDirectories.Pop();
            if (string.IsNullOrWhiteSpace(currentDirectory))
            {
                continue;
            }

            if (!TryPrepareDirectoryForScan(currentDirectory, out var preparedDirectory, out var skipReason))
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped directory reason={skipReason} path={currentDirectory}");
                continue;
            }

            if (!visitedDirectories.Add(preparedDirectory))
            {
                _logger.Info("scan", $"skipped duplicate directory path={preparedDirectory}");
                continue;
            }

            try
            {
                foreach (var executablePath in Directory.EnumerateFiles(preparedDirectory, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    scannedExecutableCount++;
                    if (filterByAllowedExeNames)
                    {
                        var executableName = MatchExePatternParser.NormalizeExecutableName(Path.GetFileName(executablePath));
                        if (string.IsNullOrWhiteSpace(executableName) || allowedExeNames is null || !allowedExeNames.Contains(executableName))
                        {
                            continue;
                        }
                    }

                    executables.Add(new DetectedExecutable
                    {
                        FileName = Path.GetFileName(executablePath),
                        FullPath = executablePath
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }
            catch (PathTooLongException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }
            catch (IOException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }

            try
            {
                foreach (var childDirectory in Directory.EnumerateDirectories(preparedDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    pendingDirectories.Push(childDirectory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }
            catch (PathTooLongException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }
            catch (IOException)
            {
                skippedDirectoryCount++;
                _logger.Warning("scan", $"skipped inaccessible directory path={preparedDirectory}");
                continue;
            }
        }

        return Task.FromResult(new ExecutableScanResult
        {
            FolderPath = normalizedFolderPath,
            Executables = executables,
            ScannedExecutableCount = scannedExecutableCount,
            SkippedDirectoryCount = skippedDirectoryCount
        });
    }

    private static bool TryPrepareDirectoryForScan(
        string directoryPath,
        out string preparedDirectory,
        out string skipReason)
    {
        try
        {
            preparedDirectory = Path.GetFullPath(directoryPath);
            var attributes = File.GetAttributes(preparedDirectory);
            if ((attributes & FileAttributes.Directory) == 0)
            {
                skipReason = "not_directory";
                return false;
            }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                skipReason = "reparse_point";
                return false;
            }

            skipReason = "";
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            preparedDirectory = directoryPath;
            skipReason = "unauthorized";
            return false;
        }
        catch (PathTooLongException)
        {
            preparedDirectory = directoryPath;
            skipReason = "path_too_long";
            return false;
        }
        catch (IOException)
        {
            preparedDirectory = directoryPath;
            skipReason = "io_error";
            return false;
        }
        catch (ArgumentException)
        {
            preparedDirectory = directoryPath;
            skipReason = "invalid_path";
            return false;
        }
        catch (NotSupportedException)
        {
            preparedDirectory = directoryPath;
            skipReason = "invalid_path";
            return false;
        }
    }
}

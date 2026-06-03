using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Components;

public interface IOptiScalerCoreInstaller
{
    OptiScalerCoreInstallResult Install(OptiScalerCoreInstallContext context);
}

public interface IOptiScalerCoreInstallFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void DeleteFile(string path);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
    void MoveFile(string sourcePath, string destinationPath, bool overwrite);
    void SetWritable(string path);
    bool IsWritable(string path);
    IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption);
}

public interface IOptiScalerCoreFileSignatureDetectors
{
    bool IsOptiScalerManagedProxyDll(string filePath);
}

public interface IOptiScalerCoreProxyDllNameResolver
{
    string Resolve(string targetPath, string preferredName);
}

public interface IOptiScalerCoreInstallLogger
{
    void Info(string category, string message);
    void Warning(string category, string message);
    void Error(string category, string message, Exception? exception = null);
}

public sealed class NullOptiScalerCoreInstallLogger : IOptiScalerCoreInstallLogger
{
    public static NullOptiScalerCoreInstallLogger Instance { get; } = new();

    private NullOptiScalerCoreInstallLogger()
    {
    }

    public void Info(string category, string message)
    {
        _ = category;
        _ = message;
    }

    public void Warning(string category, string message)
    {
        _ = category;
        _ = message;
    }

    public void Error(string category, string message, Exception? exception = null)
    {
        _ = category;
        _ = message;
        _ = exception;
    }
}

public static class OptiScalerCoreInstallErrorCodes
{
    public const string None = "";
    public const string PayloadMissing = "payload_missing";
    public const string InvalidDestination = "invalid_destination";
    public const string CopyFailed = "copy_failed";
    public const string LegacyCleanupDeleteFailed = "legacy_cleanup_delete_failed";
    public const string LegacyCleanupWritableFailed = "legacy_cleanup_writable_failed";
    public const string LegacyCleanupInvalidTarget = "legacy_cleanup_invalid_target";
}

public sealed record OptiScalerCoreInstallContext
{
    public string TargetPath { get; init; } = "";
    public string OptiScalerPayloadDirectory { get; init; } = "";
    public string FinalDllName { get; init; } = "";
    public string PreferredProxyDllName { get; init; } = "";
    public IReadOnlyList<string> ExcludePatterns { get; init; } = Array.Empty<string>();
}

public sealed record OptiScalerCoreInstallOperation
{
    public string Kind { get; init; } = "";
    public string Source { get; init; } = "";
    public string Destination { get; init; } = "";
}

public sealed record OptiScalerCoreInstallResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyList<OptiScalerCoreInstallOperation> Operations { get; init; } = Array.Empty<OptiScalerCoreInstallOperation>();

    public static OptiScalerCoreInstallResult Success(IReadOnlyList<OptiScalerCoreInstallOperation> operations)
    {
        return new OptiScalerCoreInstallResult
        {
            IsSuccess = true,
            Operations = operations
        };
    }

    public static OptiScalerCoreInstallResult Failed(string errorCode, string message = "")
    {
        return new OptiScalerCoreInstallResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

public sealed class OptiScalerCoreInstaller : IOptiScalerCoreInstaller
{
    private static readonly string[] ManagedBackupCandidates =
    [
        "OptiScaler.asi",
        "OptiScaler.dll",
        "dxgi.dll",
        "winmm.dll",
        "d3d12.dll",
        "dbghelp.dll",
        "version.dll",
        "wininet.dll",
        "winhttp.dll"
    ];

    private readonly IOptiScalerCoreInstallFileSystem _fileSystem;
    private readonly IOptiScalerCoreFileSignatureDetectors _detectors;
    private readonly IOptiScalerCoreProxyDllNameResolver _proxyResolver;
    private readonly IOptiScalerCoreInstallLogger _logger;

    public OptiScalerCoreInstaller(
        IOptiScalerCoreInstallFileSystem fileSystem,
        IOptiScalerCoreFileSignatureDetectors detectors,
        IOptiScalerCoreProxyDllNameResolver proxyResolver,
        IOptiScalerCoreInstallLogger? logger = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _proxyResolver = proxyResolver ?? throw new ArgumentNullException(nameof(proxyResolver));
        _logger = logger ?? NullOptiScalerCoreInstallLogger.Instance;
    }

    public OptiScalerCoreInstallResult Install(OptiScalerCoreInstallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedTargetPath = NormalizeTargetDirectory(context.TargetPath);
        var payload = (context.OptiScalerPayloadDirectory ?? "").Trim();
        _logger.Info(
            "Install.Core",
            $"install context target={context.TargetPath} resolved_target={normalizedTargetPath} payload={payload} final_dll={context.FinalDllName} preferred_dll={context.PreferredProxyDllName} exclude_count={context.ExcludePatterns.Count}");
        _logger.Info("Install.Core", $"target validate start target={context.TargetPath} resolved_target={normalizedTargetPath}");
        if (!_fileSystem.DirectoryExists(normalizedTargetPath))
        {
            _logger.Error("Install.Core", $"failed stage=target_validate code=invalid_target_folder target={normalizedTargetPath}");
            return OptiScalerCoreInstallResult.Failed(
                OptiScalerCoreInstallErrorCodes.InvalidDestination,
                $"Invalid target directory: {normalizedTargetPath}");
        }

        if (!_fileSystem.DirectoryExists(payload))
        {
            _logger.Error("Install.Core", $"failed stage=payload_copy code=payload_missing payload={payload}");
            return OptiScalerCoreInstallResult.Failed(
                OptiScalerCoreInstallErrorCodes.PayloadMissing,
                $"Payload directory missing: {payload}");
        }

        if (!HasRequiredPayloadFiles(payload))
        {
            _logger.Error("Install.Core", "failed stage=payload_copy code=payload_missing");
            return OptiScalerCoreInstallResult.Failed(
                OptiScalerCoreInstallErrorCodes.PayloadMissing,
                "Required payload files are missing.");
        }

        var normalizedContext = context with
        {
            TargetPath = normalizedTargetPath
        };

        string finalDll;
        try
        {
            finalDll = ResolveFinalDllName(normalizedContext);
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error(
                "Install.Core",
                $"failed stage=proxy_resolve code={OptiScalerCoreInstallErrorCodes.InvalidDestination} target={normalizedTargetPath} final_dll={context.FinalDllName} preferred_dll={context.PreferredProxyDllName}",
                ex);
            return OptiScalerCoreInstallResult.Failed(OptiScalerCoreInstallErrorCodes.InvalidDestination, ex.Message);
        }

        if (string.IsNullOrWhiteSpace(finalDll))
        {
            _logger.Error("Install.Core", $"failed stage=proxy_resolve code={OptiScalerCoreInstallErrorCodes.InvalidDestination}");
            return OptiScalerCoreInstallResult.Failed(OptiScalerCoreInstallErrorCodes.InvalidDestination);
        }

        var operations = new List<OptiScalerCoreInstallOperation>();
        try
        {
            _logger.Info("Install.Core", $"final proxy resolved {finalDll}");
            BackupManagedDlls(normalizedTargetPath, operations);
            RemoveLegacyFiles(normalizedTargetPath, operations);
            CopyPayloadTree(payload, normalizedTargetPath, operations, context.ExcludePatterns);
            RenameOptiScalerDll(normalizedTargetPath, finalDll, operations);
            var ini = Path.Combine(normalizedTargetPath, "OptiScaler.ini");
            if (!_fileSystem.FileExists(ini))
            {
                _logger.Error("Install.Core", "failed stage=payload_copy code=payload_missing");
                return OptiScalerCoreInstallResult.Failed(
                    OptiScalerCoreInstallErrorCodes.PayloadMissing,
                    "OptiScaler.ini not found after install.");
            }

            _logger.Info("Install.Core", "install success");
            return OptiScalerCoreInstallResult.Success(operations);
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error(
                "Install.Core",
                $"failed stage=rename code={OptiScalerCoreInstallErrorCodes.InvalidDestination} target={normalizedTargetPath} final_dll={finalDll}",
                ex);
            return OptiScalerCoreInstallResult.Failed(OptiScalerCoreInstallErrorCodes.InvalidDestination, ex.Message);
        }
        catch (LegacyCleanupException ex)
        {
            _logger.Error("Install.Core", $"failed stage=legacy_cleanup code={ex.ErrorCode}", ex);
            return OptiScalerCoreInstallResult.Failed(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error("Install.Core", $"failed stage=payload_copy code={OptiScalerCoreInstallErrorCodes.CopyFailed}", ex);
            return OptiScalerCoreInstallResult.Failed(OptiScalerCoreInstallErrorCodes.CopyFailed);
        }
    }

    private bool HasRequiredPayloadFiles(string payloadDir)
    {
        return _fileSystem.FileExists(Path.Combine(payloadDir, "OptiScaler.dll"))
               && _fileSystem.FileExists(Path.Combine(payloadDir, "OptiScaler.ini"));
    }

    private string ResolveFinalDllName(OptiScalerCoreInstallContext context)
    {
        var preferred = (context.FinalDllName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(preferred))
        {
            preferred = (context.PreferredProxyDllName ?? "").Trim();
        }

        return _proxyResolver.Resolve(context.TargetPath, preferred);
    }

    private void BackupManagedDlls(string targetPath, List<OptiScalerCoreInstallOperation> operations)
    {
        _logger.Info("Install.Core", "backup scan start");
        foreach (var name in ManagedBackupCandidates)
        {
            var path = Path.Combine(targetPath, name);
            if (!_fileSystem.FileExists(path))
            {
                _logger.Info("Install.Core", $"backup candidate {name} exists=false");
                continue;
            }

            if (!_detectors.IsOptiScalerManagedProxyDll(path))
            {
                _logger.Warning("Install.Core", $"backup candidate {name} exists=true managed=false");
                continue;
            }

            _logger.Info("Install.Core", $"backup candidate {name} exists=true managed=true");

            EnsureWritableIfExists(path);
            var backup = Path.Combine(targetPath, $"old_opti_{name}");
            _fileSystem.MoveFile(path, backup, overwrite: true);
            _logger.Info("Install.Core", $"backup created old_opti_{name}");
            operations.Add(new OptiScalerCoreInstallOperation { Kind = "backup", Source = path, Destination = backup });
        }
    }

    private void RemoveLegacyFiles(string targetPath, List<OptiScalerCoreInstallOperation> operations)
    {
        _logger.Info("Install.Core", "legacy cleanup start");
        foreach (var name in OptiScalerLegacyCleanupPolicy.TargetFileNames)
        {
            var path = Path.Combine(targetPath, name);
            if (_fileSystem.DirectoryExists(path))
            {
                throw new LegacyCleanupException(
                    OptiScalerCoreInstallErrorCodes.LegacyCleanupInvalidTarget,
                    $"Legacy cleanup target is a directory: {name}");
            }

            if (!_fileSystem.FileExists(path))
            {
                _logger.Info("Install.Core", $"legacy target {name} exists=false");
                continue;
            }

            _logger.Info("Install.Core", $"legacy target {name} exists=true");

            try
            {
                EnsureWritableIfExists(path);
            }
            catch (Exception ex)
            {
                throw new LegacyCleanupException(
                    OptiScalerCoreInstallErrorCodes.LegacyCleanupWritableFailed,
                    $"Failed to make legacy cleanup target writable: {name}",
                    ex);
            }

            try
            {
                _fileSystem.DeleteFile(path);
                _logger.Info("Install.Core", $"legacy delete {name} success");
            }
            catch (Exception ex)
            {
                throw new LegacyCleanupException(
                    OptiScalerCoreInstallErrorCodes.LegacyCleanupDeleteFailed,
                    $"Failed to remove legacy cleanup target: {name}",
                    ex);
            }

            operations.Add(new OptiScalerCoreInstallOperation { Kind = "delete_legacy", Source = path });
        }

        _logger.Info("Install.Core", "legacy cleanup completed");
    }

    private void CopyPayloadTree(
        string payloadRoot,
        string targetPath,
        List<OptiScalerCoreInstallOperation> operations,
        IReadOnlyList<string> excludePatterns)
    {
        var files = _fileSystem.EnumerateFiles(payloadRoot, "*", SearchOption.AllDirectories).ToArray();
        foreach (var sourceFile in files)
        {
            var relative = Path.GetRelativePath(payloadRoot, sourceFile).Replace('\\', '/');
            if (IsRequiredPayloadFile(relative))
            {
                _logger.Info("Install.Core", $"required payload preserved relative={relative}");
            }
            else if (TryResolveMatchedExcludePattern(relative, excludePatterns, out var matchedPattern))
            {
                _logger.Info("Install.Core", $"excluded relative={relative} pattern={matchedPattern}");
                continue;
            }

            var destination = CombineUnderTarget(targetPath, relative);
            var parent = Path.GetDirectoryName(destination)!;
            _fileSystem.CreateDirectory(parent);
            if (_fileSystem.FileExists(destination))
            {
                EnsureWritableIfExists(destination);
            }

            _fileSystem.CopyFile(sourceFile, destination, overwrite: true);
            operations.Add(new OptiScalerCoreInstallOperation { Kind = "copy", Source = sourceFile, Destination = destination });
        }
    }

    private static bool TryResolveMatchedExcludePattern(
        string relativePath,
        IReadOnlyList<string> patterns,
        out string matchedPattern)
    {
        matchedPattern = "";
        if (patterns.Count == 0)
        {
            return false;
        }

        var fileName = Path.GetFileName(relativePath);
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            if (pattern.Contains('/'))
            {
                if (PathMatches(relativePath, pattern))
                {
                    matchedPattern = pattern;
                    return true;
                }
            }
            else if (PathMatches(fileName, pattern) || PathMatches(relativePath, pattern))
            {
                matchedPattern = pattern;
                return true;
            }
        }

        return false;
    }

    private static bool IsRequiredPayloadFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return string.Equals(fileName, "OptiScaler.dll", StringComparison.OrdinalIgnoreCase)
               || string.Equals(fileName, "OptiScaler.ini", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathMatches(string input, string pattern)
    {
        return System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, input, ignoreCase: true);
    }

    private void RenameOptiScalerDll(string targetPath, string finalDllName, List<OptiScalerCoreInstallOperation> operations)
    {
        var source = Path.Combine(targetPath, "OptiScaler.dll");
        if (!_fileSystem.FileExists(source))
        {
            throw new InvalidOperationException("OptiScaler.dll not found in target path.");
        }

        var destination = Path.Combine(targetPath, finalDllName);
        if (_fileSystem.FileExists(destination))
        {
            EnsureWritableIfExists(destination);
            _fileSystem.DeleteFile(destination);
        }

        _fileSystem.MoveFile(source, destination, overwrite: true);
        operations.Add(new OptiScalerCoreInstallOperation { Kind = "rename", Source = source, Destination = destination });
    }

    private static string CombineUnderTarget(string targetPath, string relativePath)
    {
        return InstallerExecutionHelpers.CombineUnderTarget(targetPath, relativePath);
    }

    private void EnsureWritableIfExists(string path)
    {
        if (_fileSystem.FileExists(path) && !_fileSystem.IsWritable(path))
        {
            _fileSystem.SetWritable(path);
        }
    }

    private string NormalizeTargetDirectory(string targetPath)
    {
        var normalized = (targetPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (_fileSystem.DirectoryExists(normalized))
        {
            return normalized;
        }

        if (_fileSystem.FileExists(normalized))
        {
            var parent = Path.GetDirectoryName(normalized);
            return string.IsNullOrWhiteSpace(parent) ? "" : parent;
        }

        if (string.Equals(Path.GetExtension(normalized), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            var parent = Path.GetDirectoryName(normalized);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                return parent;
            }
        }

        return normalized;
    }

    private sealed class LegacyCleanupException : Exception
    {
        public LegacyCleanupException(string errorCode, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
    }
}

using System.IO;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;
using InfrastructureUninstall = OptiClick.Infrastructure.Install.Uninstall;

namespace OptiClick.Wpf.Install.Uninstall;

public interface IOptiClickUninstallPlanBuilder
{
    InfrastructureUninstall.UninstallPlan BuildPlan(string targetPath);
    InfrastructureUninstall.UninstallPlan BuildPlan(OptiClickUninstallPlanBuildRequest request);
}

public interface IOptiClickUninstallExecutor
{
    Task<InfrastructureUninstall.UninstallExecutionResult> ExecuteAsync(
        InfrastructureUninstall.UninstallPlan plan,
        CancellationToken cancellationToken = default);
}

public sealed record OptiClickUninstallPlanBuildRequest
{
    public string TargetPath { get; init; } = "";
    public ShellGameCardModel? SelectedGame { get; init; }
    public string FinalProxyDllName { get; init; } = "";
    public IReadOnlyList<string> UalDetectedNames { get; init; } = [];
}

public sealed class OptiClickUninstallPlanBuilder : IOptiClickUninstallPlanBuilder
{
    private readonly InfrastructureUninstall.IOptiClickUninstallPlanBuilder _inner;
    private readonly IProfilePathResolver _profilePathResolver;

    public OptiClickUninstallPlanBuilder(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors signatureDetectors,
        IFileVersionInfoReader versionInfoReader,
        IAppLogger? logger = null,
        IProfilePathResolver? profilePathResolver = null)
    {
        var appLogger = logger ?? NullAppLogger.Instance;
        _profilePathResolver = profilePathResolver ?? new ProfilePathResolver();
        _inner = new InfrastructureUninstall.OptiClickUninstallPlanBuilder(
            new FileSystemAdapter(fileSystem),
            new SignatureDetectorAdapter(signatureDetectors, versionInfoReader),
            new LoggerAdapter(appLogger));
    }

    public InfrastructureUninstall.UninstallPlan BuildPlan(string targetPath) => _inner.BuildPlan(targetPath);

    public InfrastructureUninstall.UninstallPlan BuildPlan(OptiClickUninstallPlanBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _inner.BuildPlan(new InfrastructureUninstall.UninstallPlanBuildRequest
        {
            TargetPath = request.TargetPath,
            ComponentTargets = BuildComponentTargets(request),
            EngineIniCleanupTargets = BuildEngineIniCleanupTargets(request, _profilePathResolver)
        });
    }

    private static IReadOnlyList<InfrastructureUninstall.UninstallComponentTarget> BuildComponentTargets(
        OptiClickUninstallPlanBuildRequest request)
    {
        var game = request.SelectedGame;
        if (game is null)
        {
            return Array.Empty<InfrastructureUninstall.UninstallComponentTarget>();
        }

        var targets = new List<InfrastructureUninstall.UninstallComponentTarget>();
        AddOptiScalerConfigTarget(targets);
        AddReFrameworkTarget(targets, ShellGameInstallMetadataResolver.GetReFrameworkUrl(game));
        AddSpecialKTarget(targets, ShellGameInstallMetadataResolver.GetSpecialK(game), ResolveFinalProxyDllName(request, game));
        AddUltimateAsiLoaderTargets(targets, ShellGameInstallMetadataResolver.GetUltimateAsiLoader(game), request.UalDetectedNames);
        return targets
            .DistinctBy(static target => $"{target.Kind}:{NormalizeTargetKey(target.RelativePath)}")
            .ToArray();
    }

    private static void AddOptiScalerConfigTarget(
        ICollection<InfrastructureUninstall.UninstallComponentTarget> targets)
    {
        targets.Add(new InfrastructureUninstall.UninstallComponentTarget
        {
            Kind = InfrastructureUninstall.UninstallCandidateKind.OptiScaler,
            RelativePath = "OptiScaler.ini",
            RequiresSignatureValidation = false
        });
    }

    private static IReadOnlyList<InfrastructureUninstall.UninstallEngineIniCleanupTarget> BuildEngineIniCleanupTargets(
        OptiClickUninstallPlanBuildRequest request,
        IProfilePathResolver profilePathResolver)
    {
        var rows = request.SelectedGame?.InstallMetadata?.EngineIniProfileRows;
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<InfrastructureUninstall.UninstallEngineIniCleanupTarget>();
        }

        var targets = new List<InfrastructureUninstall.UninstallEngineIniCleanupTarget>();
        foreach (var row in rows)
        {
            var profilePath = RuntimeDataRowReader.GetString(row, "path");
            var section = RuntimeDataRowReader.GetString(row, "section");
            var key = RuntimeDataRowReader.GetString(row, "key");
            if (string.IsNullOrWhiteSpace(profilePath)
                || string.IsNullOrWhiteSpace(section)
                || string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var resolvedPath = profilePathResolver.Resolve(request.TargetPath, profilePath, requireExisting: false);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                continue;
            }

            targets.Add(new InfrastructureUninstall.UninstallEngineIniCleanupTarget
            {
                FullPath = resolvedPath,
                Section = section,
                Key = key
            });
        }

        return targets
            .DistinctBy(static target =>
                $"{Path.GetFullPath(target.FullPath)}|{target.Section.Trim()}|{target.Key.Trim()}")
            .ToArray();
    }

    private static void AddReFrameworkTarget(
        ICollection<InfrastructureUninstall.UninstallComponentTarget> targets,
        string destination)
    {
        AddDllTarget(targets, InfrastructureUninstall.UninstallCandidateKind.ReFramework, destination);
    }

    private static void AddSpecialKTarget(
        ICollection<InfrastructureUninstall.UninstallComponentTarget> targets,
        string specialKValue,
        string finalProxyDllName)
    {
        var normalized = NormalizeRelativePath(specialKValue);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (string.Equals(normalized, "plugins", StringComparison.OrdinalIgnoreCase))
        {
            var proxyName = Path.GetFileName((finalProxyDllName ?? "").Trim());
            if (string.IsNullOrWhiteSpace(proxyName)
                || !proxyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AddDllTarget(
                targets,
                InfrastructureUninstall.UninstallCandidateKind.SpecialK,
                $"plugins/{proxyName}");
            return;
        }

        AddDllTarget(targets, InfrastructureUninstall.UninstallCandidateKind.SpecialK, normalized);
    }

    private static void AddUltimateAsiLoaderTargets(
        ICollection<InfrastructureUninstall.UninstallComponentTarget> targets,
        bool enabled,
        IReadOnlyList<string> detectedNames)
    {
        if (!enabled)
        {
            return;
        }

        AddDllTarget(targets, InfrastructureUninstall.UninstallCandidateKind.UltimateAsiLoader, "dinput8.dll");
        foreach (var detectedName in detectedNames ?? Array.Empty<string>())
        {
            AddDllTarget(targets, InfrastructureUninstall.UninstallCandidateKind.UltimateAsiLoader, detectedName);
        }
    }

    private static void AddDllTarget(
        ICollection<InfrastructureUninstall.UninstallComponentTarget> targets,
        InfrastructureUninstall.UninstallCandidateKind kind,
        string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(normalized)
            || Path.IsPathRooted(normalized)
            || normalized.Contains("../", StringComparison.Ordinal)
            || !normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        targets.Add(new InfrastructureUninstall.UninstallComponentTarget
        {
            Kind = kind,
            RelativePath = normalized
        });
    }

    private static string NormalizeRelativePath(string value)
    {
        var normalized = (value ?? "").Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized.Trim('/');
    }

    private static string ResolveFinalProxyDllName(
        OptiClickUninstallPlanBuildRequest request,
        ShellGameCardModel game)
    {
        var requested = Path.GetFileName((request.FinalProxyDllName ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(requested)
            && requested.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return requested;
        }

        var preferred = Path.GetFileName(ShellGameInstallMetadataResolver.GetOptiScalerDllName(game));
        if (!string.IsNullOrWhiteSpace(preferred)
            && preferred.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return preferred;
        }

        return string.IsNullOrWhiteSpace(preferred) ? "dxgi.dll" : "";
    }

    private static string NormalizeTargetKey(string value) => NormalizeRelativePath(value).ToLowerInvariant();
}

public sealed class OptiClickUninstallExecutor : IOptiClickUninstallExecutor
{
    private readonly InfrastructureUninstall.IOptiClickUninstallExecutor _inner;
    private readonly IAppLogger _logger;

    public OptiClickUninstallExecutor(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors signatureDetectors,
        IFileVersionInfoReader versionInfoReader,
        IAppLogger? logger = null)
    {
        var appLogger = logger ?? NullAppLogger.Instance;
        _logger = appLogger;
        _inner = new InfrastructureUninstall.OptiClickUninstallExecutor(
            new FileSystemAdapter(fileSystem),
            new SignatureDetectorAdapter(signatureDetectors, versionInfoReader),
            new LoggerAdapter(appLogger));
    }

    public Task<InfrastructureUninstall.UninstallExecutionResult> ExecuteAsync(
        InfrastructureUninstall.UninstallPlan plan,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithEngineIniCleanupAsync(plan, cancellationToken);
    }

    private async Task<InfrastructureUninstall.UninstallExecutionResult> ExecuteWithEngineIniCleanupAsync(
        InfrastructureUninstall.UninstallPlan plan,
        CancellationToken cancellationToken)
    {
        var fileResult = await _inner.ExecuteAsync(plan, cancellationToken);
        if (plan is null
            || plan.Status != InfrastructureUninstall.UninstallPlanStatus.Ready
            || plan.EngineIniCleanupTargets.Count == 0)
        {
            return fileResult;
        }

        var cleanupResult = ExecuteEngineIniCleanup(plan.EngineIniCleanupTargets, cancellationToken);
        return MergeResults(fileResult, cleanupResult);
    }

    private EngineIniCleanupExecutionResult ExecuteEngineIniCleanup(
        IReadOnlyList<InfrastructureUninstall.UninstallEngineIniCleanupTarget> targets,
        CancellationToken cancellationToken)
    {
        var cleaned = new List<InfrastructureUninstall.UninstallCleanedEngineIniEntry>();
        var skipped = new List<InfrastructureUninstall.UninstallSkippedEngineIniEntry>();
        var failed = new List<InfrastructureUninstall.UninstallFailedEngineIniEntry>();
        var grouped = GroupEngineIniTargets(targets, skipped);
        foreach (var (filePath, sectionMap) in grouped)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(filePath))
            {
                AddSkippedEntries(
                    skipped,
                    filePath,
                    sectionMap,
                    InfrastructureUninstall.UninstallSkipReasons.ConfigTargetMissing);
                continue;
            }

            try
            {
                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch (Exception ex)
            {
                AddFailedEntries(
                    failed,
                    filePath,
                    sectionMap,
                    InfrastructureUninstall.UninstallErrorCodes.SetWritableFailed,
                    ex.Message);
                _logger.Error("Uninstall.EngineIni", $"set writable failed path={filePath}", ex);
                continue;
            }

            var removedRows = new List<ConfigProfileAppliedRow>();
            var skippedRows = new List<ConfigProfileSkippedRow>();
            try
            {
                IniProfileDocumentEditor.RemoveEntries(
                    filePath,
                    sectionMap,
                    removeEmptySections: true,
                    trackRemoved: removedRows,
                    trackSkipped: skippedRows,
                    profileName: ConfigProfileNames.EngineIniProfile);
            }
            catch (Exception ex)
            {
                AddFailedEntries(
                    failed,
                    filePath,
                    sectionMap,
                    InfrastructureUninstall.UninstallErrorCodes.ConfigCleanupFailed,
                    ex.Message);
                _logger.Error("Uninstall.EngineIni", $"cleanup failed path={filePath}", ex);
                continue;
            }

            foreach (var row in removedRows)
            {
                var (section, key) = SplitTargetKey(row.TargetKey);
                cleaned.Add(new InfrastructureUninstall.UninstallCleanedEngineIniEntry
                {
                    FullPath = filePath,
                    Section = section,
                    Key = key
                });
            }

            foreach (var row in skippedRows)
            {
                var (section, key) = SplitTargetKey(row.TargetKey);
                skipped.Add(new InfrastructureUninstall.UninstallSkippedEngineIniEntry
                {
                    FullPath = filePath,
                    Section = section,
                    Key = key,
                    Reason = InfrastructureUninstall.UninstallSkipReasons.ConfigEntryMissing
                });
            }
        }

        return new EngineIniCleanupExecutionResult(cleaned, skipped, failed);
    }

    private static Dictionary<string, Dictionary<string, IReadOnlyList<string>>> GroupEngineIniTargets(
        IReadOnlyList<InfrastructureUninstall.UninstallEngineIniCleanupTarget> targets,
        List<InfrastructureUninstall.UninstallSkippedEngineIniEntry> skipped)
    {
        var grouped = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath((target.FullPath ?? "").Trim());
            }
            catch
            {
                skipped.Add(new InfrastructureUninstall.UninstallSkippedEngineIniEntry
                {
                    FullPath = target.FullPath ?? "",
                    Section = target.Section ?? "",
                    Key = target.Key ?? "",
                    Reason = InfrastructureUninstall.UninstallSkipReasons.InvalidPath
                });
                continue;
            }

            var section = (target.Section ?? "").Trim();
            var key = (target.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullPath)
                || string.IsNullOrWhiteSpace(section)
                || string.IsNullOrWhiteSpace(key))
            {
                skipped.Add(new InfrastructureUninstall.UninstallSkippedEngineIniEntry
                {
                    FullPath = target.FullPath ?? "",
                    Section = target.Section ?? "",
                    Key = target.Key ?? "",
                    Reason = InfrastructureUninstall.UninstallSkipReasons.InvalidPath
                });
                continue;
            }

            if (!grouped.TryGetValue(fullPath, out var sections))
            {
                sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                grouped[fullPath] = sections;
            }

            if (!sections.TryGetValue(section, out var keys))
            {
                keys = new List<string>();
                sections[section] = keys;
            }

            if (!keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                keys.Add(key);
            }
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToDictionary(
                static section => section.Key,
                static section => (IReadOnlyList<string>)section.Value,
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddSkippedEntries(
        List<InfrastructureUninstall.UninstallSkippedEngineIniEntry> skipped,
        string filePath,
        IReadOnlyDictionary<string, IReadOnlyList<string>> sectionMap,
        string reason)
    {
        foreach (var (section, keys) in sectionMap)
        {
            foreach (var key in keys)
            {
                skipped.Add(new InfrastructureUninstall.UninstallSkippedEngineIniEntry
                {
                    FullPath = filePath,
                    Section = section,
                    Key = key,
                    Reason = reason
                });
            }
        }
    }

    private static void AddFailedEntries(
        List<InfrastructureUninstall.UninstallFailedEngineIniEntry> failed,
        string filePath,
        IReadOnlyDictionary<string, IReadOnlyList<string>> sectionMap,
        string errorCode,
        string message)
    {
        foreach (var (section, keys) in sectionMap)
        {
            foreach (var key in keys)
            {
                failed.Add(new InfrastructureUninstall.UninstallFailedEngineIniEntry
                {
                    FullPath = filePath,
                    Section = section,
                    Key = key,
                    ErrorCode = errorCode,
                    Message = message
                });
            }
        }
    }

    private static (string Section, string Key) SplitTargetKey(string targetKey)
    {
        var normalized = (targetKey ?? "").Trim();
        var separatorIndex = normalized.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return ("", normalized);
        }

        return (normalized[..separatorIndex], normalized[(separatorIndex + 1)..]);
    }

    private static InfrastructureUninstall.UninstallExecutionResult MergeResults(
        InfrastructureUninstall.UninstallExecutionResult fileResult,
        EngineIniCleanupExecutionResult cleanupResult)
    {
        var deletedFiles = fileResult.DeletedFiles;
        var failedFiles = fileResult.FailedFiles;
        var skippedFiles = fileResult.SkippedFiles;
        var cleanedEntries = cleanupResult.CleanedEntries;
        var failedEntries = cleanupResult.FailedEntries;
        var skippedEntries = cleanupResult.SkippedEntries;
        var fileLevelFailure = fileResult.Status == InfrastructureUninstall.UninstallExecutionStatus.Failed
                               && failedFiles.Count == 0;
        var successCount = deletedFiles.Count + cleanedEntries.Count;
        var failureCount = failedFiles.Count + failedEntries.Count + (fileLevelFailure ? 1 : 0);
        var status = (successCount, failureCount) switch
        {
            (> 0, 0) => InfrastructureUninstall.UninstallExecutionStatus.Success,
            (> 0, > 0) => InfrastructureUninstall.UninstallExecutionStatus.PartialSuccess,
            (0, > 0) => InfrastructureUninstall.UninstallExecutionStatus.Failed,
            _ => InfrastructureUninstall.UninstallExecutionStatus.NothingToRemove
        };
        var firstFailedFile = failedFiles.FirstOrDefault();
        var firstFailedEntry = failedEntries.FirstOrDefault();

        return new InfrastructureUninstall.UninstallExecutionResult
        {
            Status = status,
            DeletedFiles = deletedFiles,
            FailedFiles = failedFiles,
            SkippedFiles = skippedFiles,
            CleanedEngineIniEntries = cleanedEntries,
            SkippedEngineIniEntries = skippedEntries,
            FailedEngineIniEntries = failedEntries,
            ErrorCode = firstFailedFile?.ErrorCode
                        ?? firstFailedEntry?.ErrorCode
                        ?? (fileLevelFailure ? fileResult.ErrorCode : InfrastructureUninstall.UninstallErrorCodes.None),
            Message = firstFailedFile?.Message
                      ?? firstFailedEntry?.Message
                      ?? (fileLevelFailure ? fileResult.Message : "")
        };
    }

    private sealed record EngineIniCleanupExecutionResult(
        IReadOnlyList<InfrastructureUninstall.UninstallCleanedEngineIniEntry> CleanedEntries,
        IReadOnlyList<InfrastructureUninstall.UninstallSkippedEngineIniEntry> SkippedEntries,
        IReadOnlyList<InfrastructureUninstall.UninstallFailedEngineIniEntry> FailedEntries);
}

internal sealed class FileSystemAdapter : InfrastructureUninstall.IOptiClickUninstallFileSystem
{
    private readonly IInstallFileSystem _inner;

    public FileSystemAdapter(IInstallFileSystem inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool FileExists(string path) => _inner.FileExists(path);

    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    public bool IsWritable(string path) => _inner.IsWritable(path);

    public void SetWritable(string path) => _inner.SetWritable(path);

    public void DeleteFile(string path) => _inner.DeleteFile(path);

    public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        => _inner.EnumerateFiles(directoryPath, searchPattern, searchOption);
}

internal sealed class SignatureDetectorAdapter : InfrastructureUninstall.IOptiClickUninstallSignatureDetector
{
    private static readonly string[] MetadataKeys =
    [
        "OriginalFilename",
        "FileDescription",
        "ProductName",
        "InternalName",
        "CompanyName",
        "FileVersion",
        "ProductVersion"
    ];

    private readonly IFileSignatureDetectors _detectors;
    private readonly IFileVersionInfoReader _versionInfoReader;

    public SignatureDetectorAdapter(IFileSignatureDetectors detectors, IFileVersionInfoReader versionInfoReader)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _versionInfoReader = versionInfoReader ?? throw new ArgumentNullException(nameof(versionInfoReader));
    }

    public InfrastructureUninstall.UninstallSignatureDetection Detect(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return InfrastructureUninstall.UninstallSignatureDetection.NotMatched(
                hasVersionInfo: false,
                InfrastructureUninstall.UninstallSkipReasons.InvalidPath);
        }

        IReadOnlyDictionary<string, string> versionInfo;
        try
        {
            versionInfo = _versionInfoReader.ReadVersionStrings(filePath);
        }
        catch
        {
            return InfrastructureUninstall.UninstallSignatureDetection.NotMatched(
                hasVersionInfo: false,
                InfrastructureUninstall.UninstallSkipReasons.VersionInfoUnavailable);
        }

        var values = MetadataKeys
            .Select(key => versionInfo.TryGetValue(key, out var value) ? value : "")
            .Select(value => (value ?? "").Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (values.Length == 0)
        {
            if (IsReFrameworkNoVersionCandidate(filePath))
            {
                return InfrastructureUninstall.UninstallSignatureDetection.Matched(
                    InfrastructureUninstall.UninstallCandidateKind.ReFramework,
                    "reframework_no_version_info");
            }

            return InfrastructureUninstall.UninstallSignatureDetection.NotMatched(
                hasVersionInfo: false,
                InfrastructureUninstall.UninstallSkipReasons.VersionInfoUnavailable);
        }

        var metadataText = string.Join(' ', values).ToLowerInvariant();
        if (metadataText.Contains("reshade", StringComparison.Ordinal))
        {
            return InfrastructureUninstall.UninstallSignatureDetection.NotMatched(
                hasVersionInfo: true,
                InfrastructureUninstall.UninstallSkipReasons.ReShadeSignatureMatched);
        }

        if (_detectors.IsOptiScalerManagedProxyDll(filePath)
            || metadataText.Contains("optiscaler", StringComparison.Ordinal))
        {
            return InfrastructureUninstall.UninstallSignatureDetection.Matched(
                InfrastructureUninstall.UninstallCandidateKind.OptiScaler,
                "optiscaler");
        }

        if (_detectors.IsSpecialKDll(filePath)
            || metadataText.Contains("special k", StringComparison.Ordinal)
            || metadataText.Contains("specialk", StringComparison.Ordinal))
        {
            var matchedText = metadataText.Contains("special k", StringComparison.Ordinal)
                ? "special k"
                : "specialk";
            return InfrastructureUninstall.UninstallSignatureDetection.Matched(
                InfrastructureUninstall.UninstallCandidateKind.SpecialK,
                matchedText);
        }

        if (_detectors.IsUltimateAsiLoaderDll(filePath)
            || metadataText.Contains("ultimate asi loader", StringComparison.Ordinal))
        {
            return InfrastructureUninstall.UninstallSignatureDetection.Matched(
                InfrastructureUninstall.UninstallCandidateKind.UltimateAsiLoader,
                "ultimate asi loader");
        }

        return InfrastructureUninstall.UninstallSignatureDetection.NotMatched(
            hasVersionInfo: true,
            InfrastructureUninstall.UninstallSkipReasons.SignatureNotMatched);
    }

    private static bool IsReFrameworkNoVersionCandidate(string filePath)
    {
        var fileName = Path.GetFileName((filePath ?? "").Trim());
        return string.Equals(fileName, "ReShade64.dll", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class LoggerAdapter : InfrastructureUninstall.IOptiClickUninstallLogger
{
    private readonly IAppLogger _inner;

    public LoggerAdapter(IAppLogger inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void Info(string category, string message) => _inner.Info(category, message);

    public void Warning(string category, string message) => _inner.Warning(category, message);

    public void Error(string category, string message, Exception? exception = null)
    {
        if (exception is null)
        {
            _inner.Error(category, message);
            return;
        }

        _inner.Error(category, message, exception);
    }
}

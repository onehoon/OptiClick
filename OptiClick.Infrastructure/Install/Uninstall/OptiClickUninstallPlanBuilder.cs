using System.IO;

namespace OptiClick.Infrastructure.Install.Uninstall;

public sealed class OptiClickUninstallPlanBuilder : IOptiClickUninstallPlanBuilder
{
    private static readonly string[] AllowedExtensions = [".dll", ".asi"];
    private static readonly string[] SignaturelessOptiScalerRootExactFileNames =
    [
        "OptiScaler.ini",
        "fakenvapi.dll",
        "fakenvapi.ini",
        "fakenvapi.log"
    ];

    private readonly IOptiClickUninstallFileSystem _fileSystem;
    private readonly IOptiClickUninstallSignatureDetector _signatureDetector;
    private readonly IOptiClickUninstallLogger _logger;

    public OptiClickUninstallPlanBuilder(
        IOptiClickUninstallFileSystem fileSystem,
        IOptiClickUninstallSignatureDetector signatureDetector,
        IOptiClickUninstallLogger? logger = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _signatureDetector = signatureDetector ?? throw new ArgumentNullException(nameof(signatureDetector));
        _logger = logger ?? NullOptiClickUninstallLogger.Instance;
    }

    public UninstallPlan BuildPlan(string targetPath)
    {
        return BuildPlan(new UninstallPlanBuildRequest
        {
            TargetPath = targetPath
        });
    }

    public UninstallPlan BuildPlan(UninstallPlanBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targetPath = request.TargetPath;
        var normalizedTargetPath = OptiClickUninstallPathHelper.NormalizeTargetDirectory(targetPath, _fileSystem);
        _logger.Info(
            "Uninstall.Validate",
            $"plan build start target={targetPath} resolved_target={normalizedTargetPath}");

        if (string.IsNullOrWhiteSpace(normalizedTargetPath) || !_fileSystem.DirectoryExists(normalizedTargetPath))
        {
            _logger.Warning("Uninstall.Validate", "plan build failed reason=invalid_target");
            return new UninstallPlan
            {
                Status = UninstallPlanStatus.InvalidTarget,
                TargetPath = normalizedTargetPath,
                ErrorCode = UninstallErrorCodes.InvalidTarget,
                Message = "Target path is invalid or not a directory."
            };
        }

        var candidates = new List<UninstallCandidate>();
        var skipped = new List<UninstallSkippedFile>();
        IReadOnlyList<string> filesToScan;
        var exactTargets = BuildExactTargetMap(normalizedTargetPath, request.ComponentTargets, skipped);
        try
        {
            var topLevelFiles = _fileSystem
                .EnumerateFiles(normalizedTargetPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Concat(_fileSystem.EnumerateFiles(normalizedTargetPath, "*.asi", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var exactExistingFiles = exactTargets.Keys
                .Where(_fileSystem.FileExists)
                .ToArray();
            filesToScan = topLevelFiles
                .Concat(exactExistingFiles)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error("Uninstall.Scan", "scan failed while enumerating files", ex);
            return new UninstallPlan
            {
                Status = UninstallPlanStatus.ValidationFailed,
                TargetPath = normalizedTargetPath,
                ErrorCode = UninstallErrorCodes.ValidationFailed,
                Message = "Failed to scan target directory."
            };
        }

        foreach (var filePath in filesToScan)
        {
            var fullPathHint = ResolveFullPathHint(filePath);
            var allowSubdirectories = !string.IsNullOrWhiteSpace(fullPathHint)
                                      && exactTargets.ContainsKey(fullPathHint);
            if (!TryNormalizeCandidate(
                    normalizedTargetPath,
                    filePath,
                    allowSubdirectories,
                    out var fullPath,
                    out var relativePath,
                    out var reason))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = filePath,
                    Reason = reason
                });
                continue;
            }

            var signaturelessExactTarget = ResolveSignaturelessExactTarget(fullPath, exactTargets);
            if (!IsAllowedExtension(fullPath) && signaturelessExactTarget is null)
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = fullPath,
                    Reason = UninstallSkipReasons.InvalidExtension
                });
                continue;
            }

            UninstallCandidateKind candidateKind;
            string matchedText;
            var requiresSignatureValidation = true;
            if (signaturelessExactTarget is not null)
            {
                candidateKind = signaturelessExactTarget.Kind;
                matchedText = "exact_filename";
                requiresSignatureValidation = false;
            }
            else
            {
                var detection = _signatureDetector.Detect(fullPath);
                if (!ShouldIncludeDetection(fullPath, detection, exactTargets, out candidateKind, out var skipReason))
                {
                    skipped.Add(new UninstallSkippedFile
                    {
                        FullPath = fullPath,
                        Reason = skipReason
                    });
                    continue;
                }

                matchedText = detection.MatchedText;
            }

            var isReadOnly = false;
            try
            {
                isReadOnly = !_fileSystem.IsWritable(fullPath);
            }
            catch
            {
                isReadOnly = false;
            }

            var candidate = new UninstallCandidate
            {
                FullPath = fullPath,
                RelativePath = relativePath,
                Kind = candidateKind,
                MatchedText = matchedText,
                IsReadOnly = isReadOnly,
                RequiresSignatureValidation = requiresSignatureValidation
            };

            _logger.Info(
                "Uninstall.Scan",
                $"candidate kind={candidate.Kind} relative={candidate.RelativePath} matched={candidate.MatchedText}");
            candidates.Add(candidate);
        }

        var engineIniCleanupTargets = NormalizeEngineIniCleanupTargets(request.EngineIniCleanupTargets);
        var status = candidates.Count == 0 && engineIniCleanupTargets.Count == 0
            ? UninstallPlanStatus.NothingToRemove
            : UninstallPlanStatus.Ready;
        _logger.Info(
            "Uninstall.Validate",
            $"plan build completed status={status} scanned={filesToScan.Count} candidates={candidates.Count} engine_ini_cleanup={engineIniCleanupTargets.Count} skipped={skipped.Count}");

        return new UninstallPlan
        {
            Status = status,
            TargetPath = normalizedTargetPath,
            Candidates = candidates,
            EngineIniCleanupTargets = engineIniCleanupTargets,
            SkippedFiles = skipped,
            ErrorCode = UninstallErrorCodes.None,
            Message = status == UninstallPlanStatus.NothingToRemove ? "No uninstall targets found." : ""
        };
    }

    private static IReadOnlyList<UninstallEngineIniCleanupTarget> NormalizeEngineIniCleanupTargets(
        IReadOnlyList<UninstallEngineIniCleanupTarget>? targets)
    {
        if (targets is null || targets.Count == 0)
        {
            return Array.Empty<UninstallEngineIniCleanupTarget>();
        }

        var normalized = new List<UninstallEngineIniCleanupTarget>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            var fullPath = ResolveFullPathHint(target.FullPath);
            var section = (target.Section ?? "").Trim();
            var key = (target.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullPath)
                || string.IsNullOrWhiteSpace(section)
                || string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var dedupeKey = $"{fullPath}|{section}|{key}";
            if (!seen.Add(dedupeKey))
            {
                continue;
            }

            normalized.Add(new UninstallEngineIniCleanupTarget
            {
                FullPath = fullPath,
                Section = section,
                Key = key
            });
        }

        return normalized;
    }

    private static Dictionary<string, List<ExactTargetRule>> BuildExactTargetMap(
        string targetPath,
        IReadOnlyList<UninstallComponentTarget> targets,
        ICollection<UninstallSkippedFile> skipped)
    {
        var map = new Dictionary<string, List<ExactTargetRule>>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets ?? Array.Empty<UninstallComponentTarget>())
        {
            if (!TryResolveComponentTarget(targetPath, target.RelativePath, out var fullPath, out var reason))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = target.RelativePath,
                    Reason = reason
                });
                continue;
            }

            if (!IsAllowedExactTarget(fullPath, target))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = fullPath,
                    Reason = UninstallSkipReasons.InvalidExtension
                });
                continue;
            }

            if (!map.TryGetValue(fullPath, out var rules))
            {
                rules = [];
                map[fullPath] = rules;
            }

            var rule = new ExactTargetRule(
                target.Kind,
                target.RequiresSignatureValidation,
                NormalizeComponentTargetRelativePath(target.RelativePath));
            if (!rules.Contains(rule))
            {
                rules.Add(rule);
            }
        }

        return map;
    }

    private static bool TryNormalizeCandidate(
        string targetPath,
        string candidatePath,
        bool allowSubdirectories,
        out string fullPath,
        out string relativePath,
        out string reason)
    {
        fullPath = "";
        relativePath = "";
        reason = "";

        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            reason = UninstallSkipReasons.InvalidPath;
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(candidatePath);
            if (!OptiClickUninstallPathHelper.IsPathUnderRoot(targetPath, fullPath))
            {
                reason = UninstallSkipReasons.OutsideTarget;
                return false;
            }

            var parent = Path.GetDirectoryName(fullPath) ?? "";
            var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!allowSubdirectories
                && !string.Equals(normalizedParent, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                reason = UninstallSkipReasons.SubdirectoryNotAllowed;
                return false;
            }

            relativePath = Path.GetRelativePath(targetPath, fullPath).Replace('\\', '/');
            return true;
        }
        catch
        {
            reason = UninstallSkipReasons.InvalidPath;
            return false;
        }
    }

    private static bool IsAllowedExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsAllowedExactTarget(string fullPath, UninstallComponentTarget target)
    {
        return IsAllowedExtension(fullPath)
               || IsSignaturelessOptiScalerRootExactTarget(fullPath, target);
    }

    private static bool IsSignaturelessOptiScalerRootExactTarget(string fullPath, UninstallComponentTarget target)
    {
        if (target.RequiresSignatureValidation || target.Kind != UninstallCandidateKind.OptiScaler)
        {
            return false;
        }

        var normalizedRelativePath = NormalizeComponentTargetRelativePath(target.RelativePath);
        return IsSignaturelessOptiScalerRootExactFileName(normalizedRelativePath)
               && string.Equals(Path.GetFileName(fullPath), normalizedRelativePath, StringComparison.OrdinalIgnoreCase);
    }

    private static ExactTargetRule? ResolveSignaturelessExactTarget(
        string fullPath,
        IReadOnlyDictionary<string, List<ExactTargetRule>> exactTargets)
    {
        return exactTargets.TryGetValue(fullPath, out var rules)
            ? rules.FirstOrDefault(rule =>
                !rule.RequiresSignatureValidation
                && rule.Kind == UninstallCandidateKind.OptiScaler
                && IsSignaturelessOptiScalerRootExactFileName(rule.RelativePath)
                && string.Equals(Path.GetFileName(fullPath), rule.RelativePath, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    private static bool ShouldIncludeDetection(
        string fullPath,
        UninstallSignatureDetection detection,
        IReadOnlyDictionary<string, List<ExactTargetRule>> exactTargets,
        out UninstallCandidateKind candidateKind,
        out string skipReason)
    {
        candidateKind = detection.Kind;
        skipReason = "";
        if (!detection.IsMatch)
        {
            if (ExactTargetsContainKind(exactTargets, fullPath, UninstallCandidateKind.ReFramework)
                && string.Equals(
                    ResolveDetectionSkipReason(detection),
                    UninstallSkipReasons.VersionInfoUnavailable,
                    StringComparison.OrdinalIgnoreCase))
            {
                candidateKind = UninstallCandidateKind.ReFramework;
                return true;
            }

            skipReason = ResolveDetectionSkipReason(detection);
            return false;
        }

        if (detection.Kind == UninstallCandidateKind.OptiScaler)
        {
            return true;
        }

        if (ExactTargetsContainKind(exactTargets, fullPath, detection.Kind))
        {
            return true;
        }

        skipReason = UninstallSkipReasons.ComponentNotRequested;
        return false;
    }

    private static string ResolveFullPathHint(string path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path);
        }
        catch
        {
            return "";
        }
    }

    private static bool TryResolveComponentTarget(
        string targetPath,
        string relativePath,
        out string fullPath,
        out string reason)
    {
        fullPath = "";
        reason = "";
        var normalizedRelative = NormalizeComponentTargetRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(normalizedRelative)
            || Path.IsPathRooted(normalizedRelative))
        {
            reason = UninstallSkipReasons.InvalidPath;
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(Path.Combine(targetPath, normalizedRelative));
            if (!OptiClickUninstallPathHelper.IsPathUnderRoot(targetPath, fullPath))
            {
                reason = UninstallSkipReasons.OutsideTarget;
                return false;
            }

            return true;
        }
        catch
        {
            reason = UninstallSkipReasons.InvalidPath;
            return false;
        }
    }

    private static string NormalizeComponentTargetRelativePath(string relativePath)
    {
        var normalizedRelative = (relativePath ?? "").Trim().Replace('\\', '/');
        while (normalizedRelative.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedRelative = normalizedRelative[2..];
        }

        return normalizedRelative.Trim('/');
    }

    private static bool IsSignaturelessOptiScalerRootExactFileName(string fileName)
    {
        return SignaturelessOptiScalerRootExactFileNames.Contains(
            (fileName ?? "").Trim(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool ExactTargetsContainKind(
        IReadOnlyDictionary<string, List<ExactTargetRule>> exactTargets,
        string fullPath,
        UninstallCandidateKind kind)
    {
        return exactTargets.TryGetValue(fullPath, out var rules)
               && rules.Any(rule => rule.Kind == kind);
    }

    private static string ResolveDetectionSkipReason(UninstallSignatureDetection detection)
    {
        if (!string.IsNullOrWhiteSpace(detection.Reason))
        {
            return detection.Reason;
        }

        return detection.IsVersionInfoAvailable
            ? UninstallSkipReasons.SignatureNotMatched
            : UninstallSkipReasons.VersionInfoUnavailable;
    }

    private sealed record ExactTargetRule(
        UninstallCandidateKind Kind,
        bool RequiresSignatureValidation,
        string RelativePath);
}

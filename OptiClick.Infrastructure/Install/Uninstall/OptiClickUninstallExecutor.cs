using System.IO;

namespace OptiClick.Infrastructure.Install.Uninstall;

public sealed class OptiClickUninstallExecutor : IOptiClickUninstallExecutor
{
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

    public OptiClickUninstallExecutor(
        IOptiClickUninstallFileSystem fileSystem,
        IOptiClickUninstallSignatureDetector signatureDetector,
        IOptiClickUninstallLogger? logger = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _signatureDetector = signatureDetector ?? throw new ArgumentNullException(nameof(signatureDetector));
        _logger = logger ?? NullOptiClickUninstallLogger.Instance;
    }

    public Task<UninstallExecutionResult> ExecuteAsync(UninstallPlan plan, CancellationToken cancellationToken = default)
    {
        if (plan is null)
        {
            return Task.FromResult(new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.Failed,
                ErrorCode = UninstallErrorCodes.InvalidPlanStatus,
                Message = "Uninstall plan is null."
            });
        }

        if (plan.Status != UninstallPlanStatus.Ready)
        {
            if (plan.Status == UninstallPlanStatus.NothingToRemove)
            {
                return Task.FromResult(new UninstallExecutionResult
                {
                    Status = UninstallExecutionStatus.NothingToRemove,
                    SkippedFiles = plan.SkippedFiles
                });
            }

            return Task.FromResult(new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.Failed,
                SkippedFiles = plan.SkippedFiles,
                ErrorCode = UninstallErrorCodes.InvalidPlanStatus,
                Message = $"Invalid plan status: {plan.Status}"
            });
        }

        if (plan.Candidates.Count == 0)
        {
            return Task.FromResult(new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.NothingToRemove,
                SkippedFiles = plan.SkippedFiles
            });
        }

        var normalizedTarget = OptiClickUninstallPathHelper.NormalizeTargetDirectory(plan.TargetPath, _fileSystem);
        if (string.IsNullOrWhiteSpace(normalizedTarget) || !_fileSystem.DirectoryExists(normalizedTarget))
        {
            return Task.FromResult(new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.Failed,
                SkippedFiles = plan.SkippedFiles,
                ErrorCode = UninstallErrorCodes.InvalidTarget,
                Message = "Target directory is invalid."
            });
        }

        _logger.Info(
            "Uninstall.Execute",
            $"execution start target={normalizedTarget} candidates={plan.Candidates.Count} skipped={plan.SkippedFiles.Count}");

        var deleted = new List<UninstallDeletedFile>();
        var failed = new List<UninstallFailedFile>();
        var skipped = new List<UninstallSkippedFile>(plan.SkippedFiles);

        foreach (var candidate in plan.Candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryNormalizeCandidate(normalizedTarget, candidate.FullPath, out var fullPath, out var relativePath, out var reason))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = candidate.FullPath,
                    Reason = reason
                });
                continue;
            }

            if (!_fileSystem.FileExists(fullPath))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = fullPath,
                    Reason = UninstallSkipReasons.MissingAtExecution
                });
                continue;
            }

            if (_fileSystem.DirectoryExists(fullPath))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = fullPath,
                    Reason = UninstallSkipReasons.DirectoryAtExecution
                });
                continue;
            }

            if (candidate.RequiresSignatureValidation)
            {
                var detection = _signatureDetector.Detect(fullPath);
                if (!IsSignatureStillEligible(candidate, detection))
                {
                    skipped.Add(new UninstallSkippedFile
                    {
                        FullPath = fullPath,
                        Reason = UninstallSkipReasons.SignatureChanged
                    });
                    continue;
                }
            }
            else if (!CanDeleteWithoutSignatureValidation(candidate, relativePath))
            {
                skipped.Add(new UninstallSkippedFile
                {
                    FullPath = fullPath,
                    Reason = UninstallSkipReasons.SignatureValidationRequired
                });
                continue;
            }

            try
            {
                if (!_fileSystem.IsWritable(fullPath))
                {
                    _fileSystem.SetWritable(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Uninstall.Execute", $"set writable failed path={relativePath}", ex);
                failed.Add(new UninstallFailedFile
                {
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    Kind = candidate.Kind,
                    ErrorCode = UninstallErrorCodes.SetWritableFailed,
                    Message = ex.Message
                });
                continue;
            }

            try
            {
                _fileSystem.DeleteFile(fullPath);
                _logger.Info("Uninstall.Execute", $"delete success path={relativePath} kind={candidate.Kind}");
                deleted.Add(new UninstallDeletedFile
                {
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    Kind = candidate.Kind
                });
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.Warning("Uninstall.Execute", $"delete blocked path={relativePath} error={UninstallErrorCodes.LockedOrPermissionDenied}");
                failed.Add(new UninstallFailedFile
                {
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    Kind = candidate.Kind,
                    ErrorCode = UninstallErrorCodes.LockedOrPermissionDenied,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Uninstall.Execute", $"delete failed path={relativePath}", ex);
                failed.Add(new UninstallFailedFile
                {
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    Kind = candidate.Kind,
                    ErrorCode = UninstallErrorCodes.DeleteFailed,
                    Message = ex.Message
                });
            }
        }

        var result = BuildFinalResult(deleted, failed, skipped);
        _logger.Info(
            "Uninstall.Execute",
            $"execution completed status={result.Status} deleted={deleted.Count} failed={failed.Count} skipped={skipped.Count}");
        return Task.FromResult(result);
    }

    private static UninstallExecutionResult BuildFinalResult(
        IReadOnlyList<UninstallDeletedFile> deleted,
        IReadOnlyList<UninstallFailedFile> failed,
        IReadOnlyList<UninstallSkippedFile> skipped)
    {
        if (deleted.Count > 0 && failed.Count == 0)
        {
            return new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.Success,
                DeletedFiles = deleted,
                FailedFiles = failed,
                SkippedFiles = skipped
            };
        }

        if (deleted.Count > 0 && failed.Count > 0)
        {
            return new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.PartialSuccess,
                DeletedFiles = deleted,
                FailedFiles = failed,
                SkippedFiles = skipped,
                ErrorCode = failed[0].ErrorCode,
                Message = failed[0].Message
            };
        }

        if (failed.Count > 0)
        {
            return new UninstallExecutionResult
            {
                Status = UninstallExecutionStatus.Failed,
                DeletedFiles = deleted,
                FailedFiles = failed,
                SkippedFiles = skipped,
                ErrorCode = failed[0].ErrorCode,
                Message = failed[0].Message
            };
        }

        return new UninstallExecutionResult
        {
            Status = UninstallExecutionStatus.NothingToRemove,
            DeletedFiles = deleted,
            FailedFiles = failed,
            SkippedFiles = skipped
        };
    }

    private static bool TryNormalizeCandidate(
        string normalizedTarget,
        string candidatePath,
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
            if (!OptiClickUninstallPathHelper.IsPathUnderRoot(normalizedTarget, fullPath))
            {
                reason = UninstallSkipReasons.OutsideTarget;
                return false;
            }

            relativePath = Path.GetRelativePath(normalizedTarget, fullPath).Replace('\\', '/');
            return true;
        }
        catch
        {
            reason = UninstallSkipReasons.InvalidPath;
            return false;
        }
    }

    private static bool IsSignatureStillEligible(
        UninstallCandidate candidate,
        UninstallSignatureDetection detection)
    {
        if (detection.IsMatch)
        {
            return detection.Kind == candidate.Kind;
        }

        return candidate.Kind == UninstallCandidateKind.ReFramework
               && string.Equals(
                   ResolveDetectionSkipReason(detection),
                   UninstallSkipReasons.VersionInfoUnavailable,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanDeleteWithoutSignatureValidation(UninstallCandidate candidate, string relativePath)
    {
        return candidate.Kind == UninstallCandidateKind.OptiScaler
               && SignaturelessOptiScalerRootExactFileNames.Contains(
                   (relativePath ?? "").Replace('\\', '/').Trim(),
                   StringComparer.OrdinalIgnoreCase);
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
}

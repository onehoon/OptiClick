using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public sealed class ArchiveReadinessFlowController
{
    private readonly IArchivePreparationCoordinator? _archivePreparationCoordinator;
    private readonly IOptiScalerVariantArchiveSyncService? _optiScalerVariantArchiveSyncService;

    public ArchiveReadinessFlowController(
        IArchivePreparationCoordinator? archivePreparationCoordinator,
        IOptiScalerVariantArchiveSyncService? optiScalerVariantArchiveSyncService = null)
    {
        _archivePreparationCoordinator = archivePreparationCoordinator;
        _optiScalerVariantArchiveSyncService = optiScalerVariantArchiveSyncService;
    }

    public async Task<ArchiveReadinessFlowResult> RefreshAsync(
        ArchiveReadinessFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_archivePreparationCoordinator is null)
        {
            return new ArchiveReadinessFlowResult
            {
                DidRun = false,
                IsSuccess = false
            };
        }

        var logs = new List<InstallFlowLogEntry>();

        try
        {
            OptiScalerVariantSyncResult? variantSync = null;
            ArchivePreparationSnapshot optiScalerSnapshot;
            if (request.OptiScalerVariantCatalog.HasRuntimeVariants
                && _optiScalerVariantArchiveSyncService is not null)
            {
                variantSync = await _optiScalerVariantArchiveSyncService.SyncAsync(
                    request.OptiScalerVariantCatalog,
                    request.PreferredOptiScalerVariant,
                    cancellationToken);
                logs.AddRange(variantSync.Logs);
                optiScalerSnapshot = new ArchivePreparationSnapshot
                {
                    States = new Dictionary<ArchiveAssetKey, ArchivePreparationState>
                    {
                        [ArchiveAssetKey.OptiScaler] = variantSync.OptiScalerState
                    }
                };
            }
            else
            {
                optiScalerSnapshot = await _archivePreparationCoordinator.PrepareOptiScalerAsync(
                    request.ModuleDownloadLinks,
                    cancellationToken);
            }

            var startupSnapshot = await _archivePreparationCoordinator.PrepareStartupArchivesAsync(
                request.ModuleDownloadLinks,
                cancellationToken);
            var merged = ArchivePreparationSnapshotMerger.Merge(optiScalerSnapshot, startupSnapshot);
            var readiness = ApplyOptiScalerVariantReadiness(
                ArchivePreparationSnapshotMapper.ToInstallPlanSnapshot(merged),
                variantSync);

            foreach (var key in ArchivePreparationSequence.StartupReadinessOrder)
            {
                var state = merged.Get(key);
                if (ShouldLogArchiveDetail(state))
                {
                    logs.Add(Info("archive", FormatArchivePreparationLog(key, state)));
                }
            }

            logs.Add(Info(
                "archive",
                $"refresh completed all_ready={FormatBool(IsAllReady(readiness))} optiscaler={readiness.OptiScalerState} fsr4={readiness.Fsr4State} optipatcher={readiness.OptiPatcherState} specialk={readiness.SpecialKState} reframework={readiness.ReframeworkState} ual={readiness.UalState} unreal5={readiness.Unreal5State}"));

            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = true,
                Readiness = readiness,
                OptiScalerVariantSync = variantSync,
                Logs = logs
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logs.Add(Info("archive", "refresh canceled"));
            throw;
        }
        catch (Exception ex)
        {
            logs.Add(Error("archive", "refresh failed", ex));
            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                Readiness = ArchiveReadinessSnapshot.NotReady,
                Logs = logs
            };
        }
    }

    private static InstallFlowLogEntry Info(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static InstallFlowLogEntry Error(string category, string message, Exception exception)
    {
        return new InstallFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }

    private static string FormatArchivePreparationLog(ArchiveAssetKey key, ArchivePreparationState state)
    {
        var stage = state.StageStatus ?? ArchivePreparationStageStatus.Unknown;
        return $"asset={FormatAssetKey(key)} state={FormatState(state)} source={Normalize(stage.Source, "unknown")} download={Normalize(stage.Download, "unknown")} sha={Normalize(stage.Sha, "unknown")} folder={Normalize(stage.Folder, "unknown")} json={Normalize(stage.Json, "unknown")} duration_ms={FormatDuration(stage.DurationMs)} filename={Normalize(state.Filename, "-")} error={Normalize(state.ErrorMessage, "-")}";
    }

    private static bool ShouldLogArchiveDetail(ArchivePreparationState state)
    {
        return !state.Ready
               || state.Downloading
               || !string.IsNullOrWhiteSpace(state.ErrorMessage);
    }

    private static bool IsAllReady(ArchiveReadinessSnapshot readiness)
    {
        return readiness.OptiScalerState == ArchiveReadinessState.Ready
               && readiness.Fsr4State == ArchiveReadinessState.Ready
               && readiness.OptiPatcherState == ArchiveReadinessState.Ready
               && readiness.SpecialKState == ArchiveReadinessState.Ready
               && readiness.ReframeworkState == ArchiveReadinessState.Ready
               && readiness.UalState == ArchiveReadinessState.Ready
               && readiness.Unreal5State == ArchiveReadinessState.Ready;
    }

    private static ArchiveReadinessSnapshot ApplyOptiScalerVariantReadiness(
        ArchiveReadinessSnapshot readiness,
        OptiScalerVariantSyncResult? variantSync)
    {
        if (variantSync is null)
        {
            return readiness;
        }

        return readiness with
        {
            OptiScalerVariant = variantSync.EffectiveVariant,
            OptiScalerVersion = variantSync.EffectiveVersion,
            OptiScalerDisplayVersion = variantSync.EffectiveDisplayVersion
        };
    }

    private static string FormatAssetKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => ArchiveAssetRuntimeDataKeys.OptiScaler,
            ArchiveAssetKey.Fsr4 => ArchiveAssetRuntimeDataKeys.Fsr4,
            ArchiveAssetKey.OptiPatcher => ArchiveAssetRuntimeDataKeys.OptiPatcher,
            ArchiveAssetKey.SpecialK => ArchiveAssetRuntimeDataKeys.SpecialK,
            ArchiveAssetKey.ReFramework => ArchiveAssetRuntimeDataKeys.ReFramework,
            ArchiveAssetKey.UltimateAsiLoader => ArchiveAssetRuntimeDataKeys.UltimateAsiLoader,
            ArchiveAssetKey.Unreal5 => ArchiveAssetRuntimeDataKeys.Unreal5,
            _ => key.ToString().ToLowerInvariant()
        };
    }

    private static string FormatState(ArchivePreparationState state)
    {
        if (state.Downloading)
        {
            return "Downloading";
        }

        if (state.Ready)
        {
            return "Ready";
        }

        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            return "Failed";
        }

        return string.IsNullOrWhiteSpace(state.ArchivePath) ? "MissingSource" : "NotReady";
    }

    private static string FormatDuration(long durationMs)
    {
        return durationMs < 0 ? "-" : durationMs.ToString();
    }

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

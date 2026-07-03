using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public sealed class ArchiveReadinessFlowController
{
    private readonly IArchivePreparationCoordinator? _archivePreparationCoordinator;
    private readonly IOptiScalerVariantArchiveSyncService? _optiScalerVariantArchiveSyncService;
    private readonly OptiScalerPayloadOptiPatcherInjectionCoordinator _optiPatcherInjectionCoordinator;

    public ArchiveReadinessFlowController(
        IArchivePreparationCoordinator? archivePreparationCoordinator,
        IOptiScalerVariantArchiveSyncService? optiScalerVariantArchiveSyncService = null,
        IOptiScalerPayloadOptiPatcherInjector? optiPatcherInjector = null)
        : this(
            archivePreparationCoordinator,
            optiScalerVariantArchiveSyncService,
            new OptiScalerPayloadOptiPatcherInjectionCoordinator(optiPatcherInjector))
    {
    }

    internal ArchiveReadinessFlowController(
        IArchivePreparationCoordinator? archivePreparationCoordinator,
        IOptiScalerVariantArchiveSyncService? optiScalerVariantArchiveSyncService,
        OptiScalerPayloadOptiPatcherInjectionCoordinator? optiPatcherInjectionCoordinator)
    {
        _archivePreparationCoordinator = archivePreparationCoordinator;
        _optiScalerVariantArchiveSyncService = optiScalerVariantArchiveSyncService;
        _optiPatcherInjectionCoordinator =
            optiPatcherInjectionCoordinator ?? new OptiScalerPayloadOptiPatcherInjectionCoordinator(null);
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
            if (_optiScalerVariantArchiveSyncService is not null)
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
                logs.Add(InstallFlowLogEntryFactory.Warning("archive", "optiscaler variant sync service missing"));
                optiScalerSnapshot = new ArchivePreparationSnapshot
                {
                    States = new Dictionary<ArchiveAssetKey, ArchivePreparationState>
                    {
                        [ArchiveAssetKey.OptiScaler] = new ArchivePreparationState
                        {
                            Filename = OptiScalerVariantCatalogBuilder.VariantResourceKey,
                            Ready = false,
                            ErrorMessage = "optiscaler_variant_sync_service_missing",
                            StageStatus = ArchivePreparationStageStatuses.MissingMetadata()
                        }
                    }
                };
            }

            var startupSnapshot = await _archivePreparationCoordinator.PrepareStartupArchivesAsync(
                request.ModuleDownloadLinks,
                cancellationToken);

            var merged = ArchivePreparationSnapshotMerger.Merge(optiScalerSnapshot, startupSnapshot);
            var readiness = ApplyOptiScalerVariantReadiness(
                ArchivePreparationSnapshotMapper.ToInstallPlanSnapshot(merged),
                variantSync);

            var optiPatcherInjection = _optiPatcherInjectionCoordinator.Apply(
                readiness,
                optiScalerSnapshot,
                startupSnapshot,
                variantSync);
            if (optiPatcherInjection.Injection is not null)
            {
                logs.AddRange(ArchiveReadinessLogFormatter.FormatOptiPatcherInjectionLogs(
                    optiPatcherInjection.Injection));
            }

            readiness = optiPatcherInjection.Readiness;

            foreach (var key in ArchivePreparationSequence.StartupReadinessOrder)
            {
                var state = merged.Get(key);
                if (ArchiveReadinessLogFormatter.ShouldLogArchiveDetail(state))
                {
                    logs.Add(InstallFlowLogEntryFactory.Info(
                        "archive",
                        ArchiveReadinessLogFormatter.FormatArchivePreparationLog(key, state)));
                }
            }

            logs.Add(InstallFlowLogEntryFactory.Info(
                "archive",
                $"refresh completed all_ready={FormatBool(IsAllReady(readiness))} optiscaler={readiness.OptiScalerState} optipatcher={readiness.OptiPatcherState} specialk={readiness.SpecialKState} reframework={readiness.ReframeworkState} unreal5={readiness.Unreal5State} fsr4={readiness.Fsr4State} amdxc64={readiness.Amdxc64State}"));

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
            logs.Add(InstallFlowLogEntryFactory.Info("archive", "refresh canceled"));
            throw;
        }
        catch (Exception ex)
        {
            logs.Add(InstallFlowLogEntryFactory.Error("archive", "refresh failed", ex));
            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                Readiness = ArchiveReadinessSnapshot.NotReady,
                Logs = logs
            };
        }
    }

    private static bool IsAllReady(ArchiveReadinessSnapshot readiness)
    {
        return readiness.OptiScalerState == ArchiveReadinessState.Ready
               && readiness.OptiPatcherState == ArchiveReadinessState.Ready
               && readiness.SpecialKState == ArchiveReadinessState.Ready
               && readiness.ReframeworkState == ArchiveReadinessState.Ready
               && readiness.Unreal5State == ArchiveReadinessState.Ready
               && readiness.Fsr4State == ArchiveReadinessState.Ready
               && readiness.Amdxc64State == ArchiveReadinessState.Ready;
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
            OptiScalerDisplayVersion = variantSync.EffectiveDisplayVersion,
            OptiScalerFileVersion = variantSync.EffectiveFileVersion,
            OptiScalerProductVersion = variantSync.EffectiveProductVersion
        };
    }

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }
}

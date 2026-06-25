using System.IO;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public sealed class ArchiveReadinessFlowController
{
    private readonly IArchivePreparationCoordinator? _archivePreparationCoordinator;
    private readonly IOptiScalerVariantArchiveSyncService? _optiScalerVariantArchiveSyncService;
    private readonly IFsr4VariantArchiveSyncService? _fsr4VariantArchiveSyncService;
    private readonly IOptiScalerPayloadOptiPatcherInjector? _optiPatcherInjector;

    public ArchiveReadinessFlowController(
        IArchivePreparationCoordinator? archivePreparationCoordinator,
        IOptiScalerVariantArchiveSyncService? optiScalerVariantArchiveSyncService = null,
        IFsr4VariantArchiveSyncService? fsr4VariantArchiveSyncService = null,
        IOptiScalerPayloadOptiPatcherInjector? optiPatcherInjector = null)
    {
        _archivePreparationCoordinator = archivePreparationCoordinator;
        _optiScalerVariantArchiveSyncService = optiScalerVariantArchiveSyncService;
        _fsr4VariantArchiveSyncService = fsr4VariantArchiveSyncService;
        _optiPatcherInjector = optiPatcherInjector;
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
            Fsr4VariantSyncResult? fsr4VariantSync = null;
            if (_fsr4VariantArchiveSyncService is not null)
            {
                fsr4VariantSync = await _fsr4VariantArchiveSyncService.SyncAsync(
                    request.Fsr4VariantCatalog,
                    cancellationToken);
                logs.AddRange(fsr4VariantSync.Logs);
                startupSnapshot = ArchivePreparationSnapshotMerger.Merge(
                    startupSnapshot,
                    ToFsr4VariantSnapshot(fsr4VariantSync));
            }

            var optiPatcherInjection = InjectOptiPatcherIntoOptiScalerPayloads(
                optiScalerSnapshot,
                startupSnapshot,
                variantSync);
            if (optiPatcherInjection is not null)
            {
                logs.AddRange(FormatOptiPatcherInjectionLogs(optiPatcherInjection));
            }

            var merged = ArchivePreparationSnapshotMerger.Merge(optiScalerSnapshot, startupSnapshot);
            var readiness = ApplyOptiScalerVariantReadiness(
                ArchivePreparationSnapshotMapper.ToInstallPlanSnapshot(merged),
                variantSync);
            readiness = ApplyOptiPatcherInjectionReadiness(readiness, variantSync, optiPatcherInjection);

            foreach (var key in ArchivePreparationSequence.StartupReadinessOrder)
            {
                var state = merged.Get(key);
                if (ShouldLogArchiveDetail(state))
                {
                    logs.Add(InstallFlowLogEntryFactory.Info("archive", FormatArchivePreparationLog(key, state)));
                }
            }

            logs.Add(InstallFlowLogEntryFactory.Info(
                "archive",
                $"refresh completed all_ready={FormatBool(IsAllReady(readiness))} optiscaler={readiness.OptiScalerState} fsr4={readiness.Fsr4State} optipatcher={readiness.OptiPatcherState} specialk={readiness.SpecialKState} reframework={readiness.ReframeworkState} ual={readiness.UalState} unreal5={readiness.Unreal5State}"));

            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = true,
                Readiness = readiness,
                OptiScalerVariantSync = variantSync,
                Fsr4VariantSync = fsr4VariantSync,
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

    private static string FormatArchivePreparationLog(ArchiveAssetKey key, ArchivePreparationState state)
    {
        var stage = state.StageStatus ?? ArchivePreparationStageStatus.Unknown;
        return $"asset={FormatAssetKey(key)} state={FormatState(state)} source={Normalize(stage.Source, "unknown")} download={Normalize(stage.Download, "unknown")} sha={Normalize(stage.Sha, "unknown")} folder={Normalize(stage.Folder, "unknown")} json={Normalize(stage.Json, "unknown")} duration_ms={FormatDuration(stage.DurationMs)} filename={Normalize(state.Filename, "-")} error={Normalize(state.ErrorMessage, "-")}";
    }

    private static ArchivePreparationSnapshot ToFsr4VariantSnapshot(Fsr4VariantSyncResult result)
    {
        var safeResult = result ?? new Fsr4VariantSyncResult();
        return new ArchivePreparationSnapshot
        {
            States = new Dictionary<ArchiveAssetKey, ArchivePreparationState>
            {
                [ArchiveAssetKey.Fsr4] = safeResult.AggregateState
            },
            Fsr4VariantStates = safeResult.StatesByVariant
        };
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
            OptiScalerDisplayVersion = variantSync.EffectiveDisplayVersion,
            OptiScalerFileVersion = variantSync.EffectiveFileVersion,
            OptiScalerProductVersion = variantSync.EffectiveProductVersion
        };
    }

    private OptiScalerPayloadOptiPatcherInjectionResult? InjectOptiPatcherIntoOptiScalerPayloads(
        ArchivePreparationSnapshot optiScalerSnapshot,
        ArchivePreparationSnapshot startupSnapshot,
        OptiScalerVariantSyncResult? variantSync)
    {
        if (_optiPatcherInjector is null)
        {
            return null;
        }

        var targets = BuildOptiScalerInjectionTargets(optiScalerSnapshot, variantSync);
        if (targets.Count == 0)
        {
            return null;
        }

        return _optiPatcherInjector.Inject(new OptiScalerPayloadOptiPatcherInjectionRequest
        {
            OptiPatcherPayloadDirectory = startupSnapshot.Get(ArchiveAssetKey.OptiPatcher).ArchivePath,
            Targets = targets
        });
    }

    private static IReadOnlyList<OptiScalerPayloadOptiPatcherInjectionTarget> BuildOptiScalerInjectionTargets(
        ArchivePreparationSnapshot optiScalerSnapshot,
        OptiScalerVariantSyncResult? variantSync)
    {
        if (variantSync is not null && variantSync.Manifest.Variants.Count > 0)
        {
            return variantSync.Manifest.Variants.Values
                .Select(static entry => new OptiScalerPayloadOptiPatcherInjectionTarget
                {
                    Variant = entry.Variant,
                    CacheEntryName = entry.CacheEntry,
                    PayloadDirectory = entry.PayloadDirectory
                })
                .ToArray();
        }

        var optiScaler = optiScalerSnapshot.Get(ArchiveAssetKey.OptiScaler);
        if (string.IsNullOrWhiteSpace(optiScaler.ArchivePath))
        {
            return Array.Empty<OptiScalerPayloadOptiPatcherInjectionTarget>();
        }

        return
        [
            new OptiScalerPayloadOptiPatcherInjectionTarget
            {
                CacheEntryName = Path.GetFileName(optiScaler.ArchivePath),
                PayloadDirectory = optiScaler.ArchivePath
            }
        ];
    }

    private static ArchiveReadinessSnapshot ApplyOptiPatcherInjectionReadiness(
        ArchiveReadinessSnapshot readiness,
        OptiScalerVariantSyncResult? variantSync,
        OptiScalerPayloadOptiPatcherInjectionResult? injection)
    {
        if (injection is null)
        {
            return readiness;
        }

        var optiScalerState = readiness.OptiScalerState;
        if (optiScalerState == ArchiveReadinessState.Ready
            && (!AreAllRuntimeOptiScalerVariantsReady(variantSync) || !injection.IsReady))
        {
            optiScalerState = ArchiveReadinessState.Failed;
        }

        return readiness with
        {
            OptiScalerState = optiScalerState,
            OptiPatcherState = injection.IsReady ? ArchiveReadinessState.Ready : ArchiveReadinessState.Failed
        };
    }

    private static bool AreAllRuntimeOptiScalerVariantsReady(OptiScalerVariantSyncResult? variantSync)
    {
        return variantSync is null
               || variantSync.Manifest.Variants.Count == 0
               || variantSync.Manifest.Variants.Values.All(static entry => entry.Ready);
    }

    private static IEnumerable<InstallFlowLogEntry> FormatOptiPatcherInjectionLogs(
        OptiScalerPayloadOptiPatcherInjectionResult injection)
    {
        var injected = injection.Targets.Count(static target => target.Injected);
        var existing = injection.Targets.Count(static target => target.UsedExisting);
        yield return InstallFlowLogEntryFactory.Info(
            "archive",
            $"optipatcher injection ready={FormatBool(injection.IsReady)} targets={injection.Targets.Count} injected={injected} existing={existing} source={Normalize(injection.SourcePath, "-")} source_error={Normalize(injection.SourceErrorCode, "-")}");

        foreach (var target in injection.Targets.Where(static target => !target.Ready))
        {
            yield return InstallFlowLogEntryFactory.Warning(
                "archive",
                $"optipatcher injection target not_ready variant={Normalize(target.Variant, "-")} cache_entry={Normalize(target.CacheEntryName, "-")} error={Normalize(target.ErrorCode, "-")}");
        }
    }

    private static string FormatAssetKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => ArchiveAssetRuntimeDataKeys.OptiScaler,
            ArchiveAssetKey.Fsr4 => ArchiveAssetRuntimeDataKeys.Fsr4Variants,
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

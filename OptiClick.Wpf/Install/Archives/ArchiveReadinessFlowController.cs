using System.IO;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Archives;

public sealed class ArchiveReadinessFlowController
{
    private readonly IArchivePreparationCoordinator? _archivePreparationCoordinator;
    private readonly IOptiScalerVariantArchiveSyncService? _optiScalerVariantArchiveSyncService;
    private readonly IOptiScalerPayloadOptiPatcherInjector? _optiPatcherInjector;
    private readonly IOptiScalerPayloadAmdxc64Provisioner? _amdxc64Provisioner;
    private readonly ArchiveCachePaths? _archiveCachePaths;

    public ArchiveReadinessFlowController(
        IArchivePreparationCoordinator? archivePreparationCoordinator,
        IOptiScalerVariantArchiveSyncService? optiScalerVariantArchiveSyncService = null,
        IOptiScalerPayloadOptiPatcherInjector? optiPatcherInjector = null,
        IOptiScalerPayloadAmdxc64Provisioner? amdxc64Provisioner = null,
        ArchiveCachePaths? archiveCachePaths = null)
    {
        _archivePreparationCoordinator = archivePreparationCoordinator;
        _optiScalerVariantArchiveSyncService = optiScalerVariantArchiveSyncService;
        _optiPatcherInjector = optiPatcherInjector;
        _amdxc64Provisioner = amdxc64Provisioner;
        _archiveCachePaths = archiveCachePaths;
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

            var optiPatcherInjection = InjectOptiPatcherIntoOptiScalerPayloads(
                optiScalerSnapshot,
                startupSnapshot,
                variantSync);
            if (optiPatcherInjection is not null)
            {
                logs.AddRange(FormatOptiPatcherInjectionLogs(optiPatcherInjection));
            }

            if (ShouldProvisionAmdxc64(request.GpuBundleKey))
            {
                var amdxc64Provision = await ProvisionAmdxc64Async(
                    request,
                    variantSync,
                    cancellationToken);
                if (amdxc64Provision is not null)
                {
                    logs.AddRange(FormatAmdxc64ProvisionLogs(amdxc64Provision));
                }
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
                $"refresh completed all_ready={FormatBool(IsAllReady(readiness))} optiscaler={readiness.OptiScalerState} optipatcher={readiness.OptiPatcherState} specialk={readiness.SpecialKState} reframework={readiness.ReframeworkState} unreal5={readiness.Unreal5State} fsr4={readiness.Fsr4State}"));

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
               && readiness.OptiPatcherState == ArchiveReadinessState.Ready
               && readiness.SpecialKState == ArchiveReadinessState.Ready
               && readiness.ReframeworkState == ArchiveReadinessState.Ready
               && readiness.Unreal5State == ArchiveReadinessState.Ready
               && readiness.Fsr4State == ArchiveReadinessState.Ready;
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

    public static bool ShouldProvisionAmdxc64(string? gpuBundleKey)
    {
        return string.Equals(
            (gpuBundleKey ?? string.Empty).Trim(),
            "radeon_rx60",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Amdxc64ProvisionResult?> ProvisionAmdxc64Async(
        ArchiveReadinessFlowRequest request,
        OptiScalerVariantSyncResult? variantSync,
        CancellationToken cancellationToken)
    {
        if (_amdxc64Provisioner is null || _archiveCachePaths is null)
        {
            return new Amdxc64ProvisionResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = "amdxc64_provisioner_missing"
            };
        }

        if (!request.ModuleDownloadLinks.TryResolveLink("amdxc64", out var descriptor))
        {
            return new Amdxc64ProvisionResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = OptiScalerPayloadAmdxc64Provisioner.DescriptorMissing
            };
        }

        var targets = BuildAmdxc64ProvisionTargets(variantSync);
        return await _amdxc64Provisioner.EnsureAsync(
            new Amdxc64ProvisionRequest
            {
                ArchiveCacheRoot = _archiveCachePaths.Root,
                Descriptor = descriptor,
                Targets = targets
            },
            cancellationToken);
    }

    private static IReadOnlyList<Amdxc64ProvisionTarget> BuildAmdxc64ProvisionTargets(
        OptiScalerVariantSyncResult? variantSync)
    {
        if (variantSync is null || variantSync.Manifest.Variants.Count == 0)
        {
            return [];
        }

        return variantSync.Manifest.Variants.Values
            .Where(static entry => entry.Ready)
            .Select(static entry => new Amdxc64ProvisionTarget
            {
                Variant = entry.Variant,
                CacheEntryName = entry.CacheEntry,
                PayloadDirectory = entry.PayloadDirectory
            })
            .ToArray();
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

    private static IEnumerable<InstallFlowLogEntry> FormatAmdxc64ProvisionLogs(
        Amdxc64ProvisionResult result)
    {
        if (!result.DidRun)
        {
            yield break;
        }

        if (!result.IsSuccess)
        {
            yield return InstallFlowLogEntryFactory.Warning(
                "archive",
                $"amdxc64 provision failed error={Normalize(result.ErrorCode, "-")} targets={result.Targets.Count}");
        }

        foreach (var target in result.Targets)
        {
            if (target.AlreadyReady)
            {
                yield return InstallFlowLogEntryFactory.Info(
                    "archive",
                    $"amdxc64 provision skipped variant={Normalize(target.Variant, "-")} reason=already_ready");
                continue;
            }

            if (target.Copied)
            {
                yield return InstallFlowLogEntryFactory.Info(
                    "archive",
                    $"amdxc64 provision copied variant={Normalize(target.Variant, "-")} destination={Normalize(RelativeAmdxc64Destination(target.DestinationPath), "-")}");
                continue;
            }

            if (target.Failed)
            {
                yield return InstallFlowLogEntryFactory.Warning(
                    "archive",
                    $"amdxc64 provision failed variant={Normalize(target.Variant, "-")} error={Normalize(target.ErrorCode, "-")}");
            }
        }
    }

    private static string RelativeAmdxc64Destination(string destinationPath)
    {
        return string.IsNullOrWhiteSpace(destinationPath)
            ? ""
            : Path.Combine("OptiScaler", "amdxc64.dll");
    }

    private static string FormatAssetKey(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => ArchiveAssetRuntimeDataKeys.OptiScaler,
            ArchiveAssetKey.OptiPatcher => ArchiveAssetRuntimeDataKeys.OptiPatcher,
            ArchiveAssetKey.SpecialK => ArchiveAssetRuntimeDataKeys.SpecialK,
            ArchiveAssetKey.ReFramework => ArchiveAssetRuntimeDataKeys.ReFramework,
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

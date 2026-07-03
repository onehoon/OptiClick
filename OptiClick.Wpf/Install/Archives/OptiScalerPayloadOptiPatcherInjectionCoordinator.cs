using System.IO;

namespace OptiClick.Wpf.Install.Archives;

internal sealed class OptiScalerPayloadOptiPatcherInjectionCoordinator
{
    private readonly IOptiScalerPayloadOptiPatcherInjector? _injector;

    public OptiScalerPayloadOptiPatcherInjectionCoordinator(IOptiScalerPayloadOptiPatcherInjector? injector)
    {
        _injector = injector;
    }

    public OptiScalerPayloadOptiPatcherInjectionCoordinatorResult Apply(
        ArchiveReadinessSnapshot readiness,
        ArchivePreparationSnapshot optiScalerSnapshot,
        ArchivePreparationSnapshot startupSnapshot,
        OptiScalerVariantSyncResult? variantSync)
    {
        var injection = InjectOptiPatcherIntoOptiScalerPayloads(
            optiScalerSnapshot,
            startupSnapshot,
            variantSync);

        return new OptiScalerPayloadOptiPatcherInjectionCoordinatorResult
        {
            Injection = injection,
            Readiness = ApplyOptiPatcherInjectionReadiness(readiness, variantSync, injection)
        };
    }

    private OptiScalerPayloadOptiPatcherInjectionResult? InjectOptiPatcherIntoOptiScalerPayloads(
        ArchivePreparationSnapshot optiScalerSnapshot,
        ArchivePreparationSnapshot startupSnapshot,
        OptiScalerVariantSyncResult? variantSync)
    {
        if (_injector is null)
        {
            return null;
        }

        var targets = BuildOptiScalerInjectionTargets(optiScalerSnapshot, variantSync);
        if (targets.Count == 0)
        {
            return null;
        }

        return _injector.Inject(new OptiScalerPayloadOptiPatcherInjectionRequest
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
}

internal sealed record OptiScalerPayloadOptiPatcherInjectionCoordinatorResult
{
    public OptiScalerPayloadOptiPatcherInjectionResult? Injection { get; init; }
    public required ArchiveReadinessSnapshot Readiness { get; init; }
}

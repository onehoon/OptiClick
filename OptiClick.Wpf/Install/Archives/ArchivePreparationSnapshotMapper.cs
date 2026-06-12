using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public static class ArchivePreparationSnapshotMapper
{
    public static ArchiveReadinessSnapshot ToInstallPlanSnapshot(ArchivePreparationSnapshot snapshot)
    {
        var fsr4Variants = MapFsr4Variants(snapshot.Fsr4VariantStates);
        return new ArchiveReadinessSnapshot
        {
            OptiScalerState = MapState(snapshot.Get(ArchiveAssetKey.OptiScaler)),
            OptiScalerSourceArchive = snapshot.Get(ArchiveAssetKey.OptiScaler).ArchivePath,
            Fsr4State = ResolveFsr4AggregateState(snapshot, fsr4Variants),
            Fsr4SourceArchive = snapshot.Get(ArchiveAssetKey.Fsr4).ArchivePath,
            Fsr4Variants = fsr4Variants,
            UalState = MapState(snapshot.Get(ArchiveAssetKey.UltimateAsiLoader)),
            UalSourceArchive = snapshot.Get(ArchiveAssetKey.UltimateAsiLoader).ArchivePath,
            OptiPatcherState = MapState(snapshot.Get(ArchiveAssetKey.OptiPatcher)),
            OptiPatcherSourceArchive = snapshot.Get(ArchiveAssetKey.OptiPatcher).ArchivePath,
            SpecialKState = MapState(snapshot.Get(ArchiveAssetKey.SpecialK)),
            SpecialKSourceArchive = snapshot.Get(ArchiveAssetKey.SpecialK).ArchivePath,
            ReframeworkState = MapState(snapshot.Get(ArchiveAssetKey.ReFramework)),
            ReframeworkSourceArchive = snapshot.Get(ArchiveAssetKey.ReFramework).ArchivePath,
            Unreal5State = MapState(snapshot.Get(ArchiveAssetKey.Unreal5)),
            Unreal5SourceArchive = snapshot.Get(ArchiveAssetKey.Unreal5).ArchivePath
        };
    }

    private static IReadOnlyDictionary<string, Fsr4VariantReadiness> MapFsr4Variants(
        IReadOnlyDictionary<string, ArchivePreparationState>? states)
    {
        if (states is null || states.Count == 0)
        {
            return new Dictionary<string, Fsr4VariantReadiness>(StringComparer.OrdinalIgnoreCase);
        }

        return states.ToDictionary(
            static pair => pair.Key,
            static pair => new Fsr4VariantReadiness
            {
                Variant = pair.Key,
                State = MapState(pair.Value),
                SourceArchive = pair.Value.ArchivePath
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static ArchiveReadinessState ResolveFsr4AggregateState(
        ArchivePreparationSnapshot snapshot,
        IReadOnlyDictionary<string, Fsr4VariantReadiness> variants)
    {
        if (variants.Count == 0)
        {
            return MapState(snapshot.Get(ArchiveAssetKey.Fsr4));
        }

        return variants.Values.All(static variant => variant.State == ArchiveReadinessState.Ready)
            ? ArchiveReadinessState.Ready
            : ArchiveReadinessState.Failed;
    }

    private static ArchiveReadinessState MapState(ArchivePreparationState state)
    {
        if (state.Downloading)
        {
            return ArchiveReadinessState.Downloading;
        }

        if (state.Ready)
        {
            return ArchiveReadinessState.Ready;
        }

        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            return ArchiveReadinessState.Failed;
        }

        if (string.IsNullOrWhiteSpace(state.ArchivePath))
        {
            return ArchiveReadinessState.MissingSource;
        }

        return ArchiveReadinessState.NotReady;
    }
}

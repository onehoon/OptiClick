using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public static class ArchivePreparationSnapshotMapper
{
    public static ArchiveReadinessSnapshot ToInstallPlanSnapshot(ArchivePreparationSnapshot snapshot)
    {
        return new ArchiveReadinessSnapshot
        {
            OptiScalerState = MapState(snapshot.Get(ArchiveAssetKey.OptiScaler)),
            OptiScalerSourceArchive = snapshot.Get(ArchiveAssetKey.OptiScaler).ArchivePath,
            OptiPatcherState = MapState(snapshot.Get(ArchiveAssetKey.OptiPatcher)),
            OptiPatcherSourceArchive = snapshot.Get(ArchiveAssetKey.OptiPatcher).ArchivePath,
            SpecialKState = MapState(snapshot.Get(ArchiveAssetKey.SpecialK)),
            SpecialKSourceArchive = snapshot.Get(ArchiveAssetKey.SpecialK).ArchivePath,
            ReframeworkState = MapState(snapshot.Get(ArchiveAssetKey.ReFramework)),
            ReframeworkSourceArchive = snapshot.Get(ArchiveAssetKey.ReFramework).ArchivePath,
            Unreal5State = MapState(snapshot.Get(ArchiveAssetKey.Unreal5)),
            Unreal5SourceArchive = snapshot.Get(ArchiveAssetKey.Unreal5).ArchivePath,
            Fsr4State = MapState(snapshot.Get(ArchiveAssetKey.Fsr4)),
            Fsr4SourceArchive = snapshot.Get(ArchiveAssetKey.Fsr4).ArchivePath,
            Amdxc64State = MapState(snapshot.Get(ArchiveAssetKey.Amdxc64)),
            Amdxc64SourceArchive = snapshot.Get(ArchiveAssetKey.Amdxc64).ArchivePath
        };
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

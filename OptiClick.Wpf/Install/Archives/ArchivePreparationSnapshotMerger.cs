namespace OptiClick.Wpf.Install.Archives;

public static class ArchivePreparationSnapshotMerger
{
    public static ArchivePreparationSnapshot Merge(
        ArchivePreparationSnapshot first,
        ArchivePreparationSnapshot second)
    {
        var states = new Dictionary<ArchiveAssetKey, ArchivePreparationState>();
        foreach (var pair in first.States)
        {
            states[pair.Key] = pair.Value;
        }

        foreach (var pair in second.States)
        {
            states[pair.Key] = pair.Value;
        }

        var fsr4VariantStates = new Dictionary<string, ArchivePreparationState>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in first.Fsr4VariantStates)
        {
            fsr4VariantStates[pair.Key] = pair.Value;
        }

        foreach (var pair in second.Fsr4VariantStates)
        {
            fsr4VariantStates[pair.Key] = pair.Value;
        }

        return new ArchivePreparationSnapshot
        {
            States = states,
            Fsr4VariantStates = fsr4VariantStates
        };
    }
}

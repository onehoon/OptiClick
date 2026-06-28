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

        return new ArchivePreparationSnapshot
        {
            States = states
        };
    }
}

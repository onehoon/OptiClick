namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchivePreparationState
{
    public string Filename { get; init; } = "";
    public string ArchivePath { get; init; } = "";
    public bool Ready { get; init; }
    public bool Downloading { get; init; }
    public string ErrorMessage { get; init; } = "";
}

public sealed record ArchiveAssetPreparationResult
{
    public ArchiveAssetKey AssetKey { get; init; }
    public ArchivePreparationState State { get; init; } = new();
}

public sealed record ArchivePreparationSnapshot
{
    public static readonly ArchivePreparationSnapshot Empty = new();

    public IReadOnlyDictionary<ArchiveAssetKey, ArchivePreparationState> States { get; init; } =
        new Dictionary<ArchiveAssetKey, ArchivePreparationState>();

    public ArchivePreparationState Get(ArchiveAssetKey key)
    {
        return States.TryGetValue(key, out var state) ? state : new ArchivePreparationState();
    }
}

public sealed record ArchivePreparationSequence
{
    public static readonly IReadOnlyList<ArchiveAssetKey> DefaultStartupOrder =
    [
        ArchiveAssetKey.Fsr4,
        ArchiveAssetKey.OptiPatcher,
        ArchiveAssetKey.SpecialK,
        ArchiveAssetKey.ReFramework,
        ArchiveAssetKey.UltimateAsiLoader,
        ArchiveAssetKey.Unreal5
    ];
}

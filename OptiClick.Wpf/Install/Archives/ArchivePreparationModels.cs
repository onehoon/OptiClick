namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchivePreparationState
{
    public string Filename { get; init; } = "";
    public string ArchivePath { get; init; } = "";
    public bool Ready { get; init; }
    public bool Downloading { get; init; }
    public string ErrorMessage { get; init; } = "";
    public ArchivePreparationStageStatus StageStatus { get; init; } = ArchivePreparationStageStatus.Unknown;
}

public sealed record ArchivePreparationStageStatus
{
    public static readonly ArchivePreparationStageStatus Unknown = new();

    public string Source { get; init; } = "";
    public string Download { get; init; } = "";
    public string Sha { get; init; } = "";
    public string Folder { get; init; } = "";
    public string Json { get; init; } = "";
    public long DurationMs { get; init; } = -1;
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
    public static readonly IReadOnlyList<ArchiveAssetKey> StartupReadinessOrder =
    [
        ArchiveAssetKey.OptiScaler,
        ArchiveAssetKey.SpecialK,
        ArchiveAssetKey.ReFramework,
        ArchiveAssetKey.Unreal5,
        ArchiveAssetKey.Fsr4,
        ArchiveAssetKey.OptiPatcher
    ];

    public static readonly IReadOnlyList<ArchiveAssetKey> DefaultStartupOrder =
    [
        ArchiveAssetKey.SpecialK,
        ArchiveAssetKey.ReFramework,
        ArchiveAssetKey.Unreal5,
        ArchiveAssetKey.Fsr4,
        ArchiveAssetKey.OptiPatcher
    ];
}

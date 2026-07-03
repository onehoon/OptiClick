namespace OptiClick.Wpf.Install.Archives;

public sealed record StartupArchiveAssetDefinition
{
    public required ArchiveAssetKey Key { get; init; }
    public required string RuntimeDataKey { get; init; }
    public required string Label { get; init; }
    public required string CacheDirectoryName { get; init; }
    public bool AllowDirectPayloadFile { get; init; }
    public IReadOnlyList<string> RequiredFiles { get; init; } = [];
}

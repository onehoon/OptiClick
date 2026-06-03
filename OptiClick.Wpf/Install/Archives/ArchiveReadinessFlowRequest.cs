namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveReadinessFlowRequest
{
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public bool Fsr4Enabled { get; init; } = true;
}

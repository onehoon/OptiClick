namespace OptiClick.Core.Scan;

public sealed class ScanFolderDiscoveryEntry
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public bool IsChecked { get; init; } = true;
}

public interface IScanFolderDiscoveryService
{
    IReadOnlyList<ScanFolderDiscoveryEntry> DiscoverDefaultFolders();
}

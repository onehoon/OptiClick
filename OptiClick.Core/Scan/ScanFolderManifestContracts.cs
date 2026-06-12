namespace OptiClick.Core.Scan;

public sealed class ScanFolderManifest
{
    public int Version { get; init; } = 2;
    public List<ScanFolderManifestEntry> Folders { get; init; } = [];
    public List<ScanFolderManifestEntry> AddedFolders { get; init; } = [];
}

public sealed class ScanFolderManifestEntry
{
    public string Path { get; init; } = "";
    public bool IsChecked { get; init; } = true;
    public bool IsDefault { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}

public interface IScanFolderManifestStore
{
    IReadOnlyList<ScanFolderManifestEntry> Load();
    void Save(IReadOnlyList<ScanFolderManifestEntry> folders);
}

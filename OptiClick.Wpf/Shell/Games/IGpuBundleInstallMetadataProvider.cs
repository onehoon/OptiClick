namespace OptiClick.Wpf.Shell.Games;

public interface IGpuBundleInstallMetadataProvider
{
    bool TryGetMetadata(string gameId, out MergedGameInstallMetadata metadata);
}

public sealed class NullGpuBundleInstallMetadataProvider : IGpuBundleInstallMetadataProvider
{
    public static readonly NullGpuBundleInstallMetadataProvider Instance = new();

    private NullGpuBundleInstallMetadataProvider()
    {
    }

    public bool TryGetMetadata(string gameId, out MergedGameInstallMetadata metadata)
    {
        _ = gameId;
        metadata = MergedGameInstallMetadata.Empty;
        return false;
    }
}

public sealed class DictionaryGpuBundleInstallMetadataProvider : IGpuBundleInstallMetadataProvider
{
    private readonly IReadOnlyDictionary<string, MergedGameInstallMetadata> _metadataByGameId;

    public DictionaryGpuBundleInstallMetadataProvider(IReadOnlyDictionary<string, MergedGameInstallMetadata> metadataByGameId)
    {
        _metadataByGameId = metadataByGameId ?? new Dictionary<string, MergedGameInstallMetadata>(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetMetadata(string gameId, out MergedGameInstallMetadata metadata)
    {
        var normalized = (gameId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            metadata = MergedGameInstallMetadata.Empty;
            return false;
        }

        if (_metadataByGameId.TryGetValue(normalized, out metadata!))
        {
            metadata ??= MergedGameInstallMetadata.Empty;
            return true;
        }

        metadata = MergedGameInstallMetadata.Empty;
        return false;
    }
}


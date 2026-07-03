namespace OptiClick.Wpf.Install.Archives;

public static class StartupArchiveAssetDefinitions
{
    private static readonly StartupArchiveAssetDefinition[] VersionedStartupAssets =
    [
        new()
        {
            Key = ArchiveAssetKey.SpecialK,
            RuntimeDataKey = ArchiveAssetRuntimeDataKeys.SpecialK,
            Label = "Special K archive",
            CacheDirectoryName = "SpecialK",
            RequiredFiles = ["SpecialK64.dll"]
        },
        new()
        {
            Key = ArchiveAssetKey.ReFramework,
            RuntimeDataKey = ArchiveAssetRuntimeDataKeys.ReFramework,
            Label = "REFramework archive",
            CacheDirectoryName = "REFramework",
            RequiredFiles = ["dinput8.dll"]
        },
        new()
        {
            Key = ArchiveAssetKey.Unreal5,
            RuntimeDataKey = ArchiveAssetRuntimeDataKeys.Unreal5,
            Label = "Unreal5 patch archive",
            CacheDirectoryName = "Unreal5",
            RequiredFiles = ["dxgi.dll", "dxgi.ini"]
        },
        new()
        {
            Key = ArchiveAssetKey.Fsr4,
            RuntimeDataKey = ArchiveAssetRuntimeDataKeys.Fsr4,
            Label = "FSR4 archive",
            CacheDirectoryName = "FSR4",
            RequiredFiles = ["amd_fidelityfx_upscaler_dx12.dll"]
        },
        new()
        {
            Key = ArchiveAssetKey.Amdxc64,
            RuntimeDataKey = ArchiveAssetRuntimeDataKeys.Amdxc64,
            Label = "amdxc64 archive",
            CacheDirectoryName = "amdxc64",
            AllowDirectPayloadFile = true,
            RequiredFiles = ["amdxc64.dll"]
        }
    ];

    public static IReadOnlyList<StartupArchiveAssetDefinition> VersionedStartupArchiveAssets => VersionedStartupAssets;

    public static IReadOnlyList<ArchiveAssetKey> VersionedStartupArchiveOrder { get; } =
        VersionedStartupAssets.Select(static definition => definition.Key).ToArray();

    public static bool TryGet(ArchiveAssetKey key, out StartupArchiveAssetDefinition definition)
    {
        foreach (var candidate in VersionedStartupAssets)
        {
            if (candidate.Key == key)
            {
                definition = candidate;
                return true;
            }
        }

        definition = null!;
        return false;
    }

    public static StartupArchiveAssetDefinition Get(ArchiveAssetKey key)
    {
        return TryGet(key, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "Archive asset is not a versioned startup archive.");
    }

    public static IArchivePayloadValidator CreateValidator(ArchiveAssetKey key)
    {
        return TryGet(key, out var definition)
            ? new RequiredFilesPayloadValidator(definition.RequiredFiles)
            : new NonEmptyPayloadValidator();
    }
}

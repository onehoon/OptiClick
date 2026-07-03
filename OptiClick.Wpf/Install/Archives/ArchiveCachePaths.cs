using System.IO;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveCachePaths
{
    private const string ArchiveScopeDirectoryName = "Archives_0_10";

    public required string Root { get; init; }
    public required string ManifestRoot { get; init; }
    public required string OptiScalerCacheDir { get; init; }
    public required string OptiPatcherCacheDir { get; init; }
    public required string SpecialKCacheDir { get; init; }
    public required string ReFrameworkCacheDir { get; init; }
    public required string Unreal5CacheDir { get; init; }
    public required string Fsr4CacheDir { get; init; }
    public required string Amdxc64CacheDir { get; init; }
    public required string OptiScalerPayloadCacheRoot { get; init; }

    public static ArchiveCachePaths CreateDefault(IAppLocalDataPathProvider? localDataPathProvider = null)
    {
        var pathProvider = localDataPathProvider ?? new AppLocalDataPathProvider();
        var archivesRoot = Path.Combine(pathProvider.RootDirectory, ArchiveScopeDirectoryName);
        var manifestRoot = Path.Combine(pathProvider.ManifestDirectory, ArchiveScopeDirectoryName);
        var optiScalerArchiveRoot = Path.Combine(archivesRoot, "OptiScaler");

        return new ArchiveCachePaths
        {
            Root = archivesRoot,
            ManifestRoot = manifestRoot,
            OptiScalerCacheDir = optiScalerArchiveRoot,
            OptiPatcherCacheDir = Path.Combine(archivesRoot, "OptiPatcher"),
            SpecialKCacheDir = ResolveVersionedCacheDir(archivesRoot, ArchiveAssetKey.SpecialK),
            ReFrameworkCacheDir = ResolveVersionedCacheDir(archivesRoot, ArchiveAssetKey.ReFramework),
            Unreal5CacheDir = ResolveVersionedCacheDir(archivesRoot, ArchiveAssetKey.Unreal5),
            Fsr4CacheDir = ResolveVersionedCacheDir(archivesRoot, ArchiveAssetKey.Fsr4),
            Amdxc64CacheDir = ResolveVersionedCacheDir(archivesRoot, ArchiveAssetKey.Amdxc64),
            OptiScalerPayloadCacheRoot = optiScalerArchiveRoot
        };
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ManifestRoot);
        Directory.CreateDirectory(OptiScalerCacheDir);
        Directory.CreateDirectory(OptiPatcherCacheDir);
        Directory.CreateDirectory(SpecialKCacheDir);
        Directory.CreateDirectory(ReFrameworkCacheDir);
        Directory.CreateDirectory(Unreal5CacheDir);
        Directory.CreateDirectory(Fsr4CacheDir);
        Directory.CreateDirectory(Amdxc64CacheDir);
        Directory.CreateDirectory(OptiScalerPayloadCacheRoot);
    }

    public string ResolveCacheDirectory(ArchiveAssetKey key)
    {
        if (StartupArchiveAssetDefinitions.TryGet(key, out var definition))
        {
            return Path.Combine(Root, definition.CacheDirectoryName);
        }

        return key switch
        {
            ArchiveAssetKey.OptiScaler => OptiScalerCacheDir,
            ArchiveAssetKey.OptiPatcher => OptiPatcherCacheDir,
            _ => Root
        };
    }

    private static string ResolveVersionedCacheDir(string archivesRoot, ArchiveAssetKey key)
    {
        return Path.Combine(archivesRoot, StartupArchiveAssetDefinitions.Get(key).CacheDirectoryName);
    }
}

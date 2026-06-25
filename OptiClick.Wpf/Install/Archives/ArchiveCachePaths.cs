using System.IO;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveCachePaths
{
    public required string Root { get; init; }
    public required string ManifestRoot { get; init; }
    public required string OptiScalerCacheDir { get; init; }
    public required string Fsr4CacheDir { get; init; }
    public required string OptiPatcherCacheDir { get; init; }
    public required string SpecialKCacheDir { get; init; }
    public required string ReFrameworkCacheDir { get; init; }
    public required string Unreal5CacheDir { get; init; }
    public required string OptiScalerPayloadCacheRoot { get; init; }

    public static ArchiveCachePaths CreateDefault(IAppLocalDataPathProvider? localDataPathProvider = null)
    {
        var pathProvider = localDataPathProvider ?? new AppLocalDataPathProvider();
        var archivesRoot = Path.Combine(pathProvider.RootDirectory, "ArchivesV2");
        var manifestRoot = Path.Combine(pathProvider.ManifestDirectory, "ArchivesV2");
        var optiScalerArchiveRoot = Path.Combine(archivesRoot, "OptiScaler");

        return new ArchiveCachePaths
        {
            Root = archivesRoot,
            ManifestRoot = manifestRoot,
            OptiScalerCacheDir = optiScalerArchiveRoot,
            Fsr4CacheDir = Path.Combine(archivesRoot, "FSR4"),
            OptiPatcherCacheDir = Path.Combine(archivesRoot, "OptiPatcher"),
            SpecialKCacheDir = Path.Combine(archivesRoot, "SpecialK"),
            ReFrameworkCacheDir = Path.Combine(archivesRoot, "REFramework"),
            Unreal5CacheDir = Path.Combine(archivesRoot, "Unreal5"),
            OptiScalerPayloadCacheRoot = optiScalerArchiveRoot
        };
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ManifestRoot);
        Directory.CreateDirectory(OptiScalerCacheDir);
        Directory.CreateDirectory(Fsr4CacheDir);
        Directory.CreateDirectory(OptiPatcherCacheDir);
        Directory.CreateDirectory(SpecialKCacheDir);
        Directory.CreateDirectory(ReFrameworkCacheDir);
        Directory.CreateDirectory(Unreal5CacheDir);
        Directory.CreateDirectory(OptiScalerPayloadCacheRoot);
    }

    public string ResolveCacheDirectory(ArchiveAssetKey key)
    {
        return key switch
        {
            ArchiveAssetKey.OptiScaler => OptiScalerCacheDir,
            ArchiveAssetKey.Fsr4 => Fsr4CacheDir,
            ArchiveAssetKey.OptiPatcher => OptiPatcherCacheDir,
            ArchiveAssetKey.SpecialK => SpecialKCacheDir,
            ArchiveAssetKey.ReFramework => ReFrameworkCacheDir,
            ArchiveAssetKey.Unreal5 => Unreal5CacheDir,
            _ => Root
        };
    }
}

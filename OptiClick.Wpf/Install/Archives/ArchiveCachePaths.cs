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
    public required string UltimateAsiLoaderCacheDir { get; init; }
    public required string Unreal5CacheDir { get; init; }
    public required string OptiScalerPayloadCacheRoot { get; init; }

    public static ArchiveCachePaths CreateDefault(IAppLocalDataPathProvider? localDataPathProvider = null)
    {
        var pathProvider = localDataPathProvider ?? new AppLocalDataPathProvider();
        var archivesRoot = pathProvider.ArchivesDirectory;
        var optiScalerArchiveRoot = Path.Combine(archivesRoot, "optiscaler");

        return new ArchiveCachePaths
        {
            Root = archivesRoot,
            ManifestRoot = pathProvider.ManifestDirectory,
            OptiScalerCacheDir = optiScalerArchiveRoot,
            Fsr4CacheDir = Path.Combine(archivesRoot, "fsr4"),
            OptiPatcherCacheDir = Path.Combine(archivesRoot, "optipatcher"),
            SpecialKCacheDir = Path.Combine(archivesRoot, "specialk"),
            ReFrameworkCacheDir = Path.Combine(archivesRoot, "reframework"),
            UltimateAsiLoaderCacheDir = Path.Combine(archivesRoot, "ual"),
            Unreal5CacheDir = Path.Combine(archivesRoot, "unreal5"),
            OptiScalerPayloadCacheRoot = Path.Combine(optiScalerArchiveRoot, "payload")
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
        Directory.CreateDirectory(UltimateAsiLoaderCacheDir);
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
            ArchiveAssetKey.UltimateAsiLoader => UltimateAsiLoaderCacheDir,
            ArchiveAssetKey.Unreal5 => Unreal5CacheDir,
            _ => Root
        };
    }
}

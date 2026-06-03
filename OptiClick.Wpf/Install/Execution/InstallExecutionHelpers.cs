using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public static class InstallExecutionHelpers
{
    public static string ExtractModuleUrl(IReadOnlyDictionary<string, object?> moduleDownloadLinks, string moduleKey)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.ExtractModuleUrl(moduleDownloadLinks, moduleKey);
    }

    public static string ReadString(IReadOnlyDictionary<string, object?> values, string key)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.ReadString(values, key);
    }

    public static string NormalizeRelativeDllPath(string destinationRelPath)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.NormalizeRelativeDllPath(destinationRelPath);
    }

    public static string CombineUnderTarget(string targetPath, string relativePath)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.CombineUnderTarget(targetPath, relativePath);
    }

    public static string ResolveDownloadFileName(string url, string requestedFileName, string fallback)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.ResolveDownloadFileName(url, requestedFileName, fallback);
    }

    public static string NormalizeAlias(string value)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.NormalizeAlias(value);
    }

    public static string NormalizeCacheEntryName(string input, string fallback)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.NormalizeCacheEntryName(input, fallback);
    }

    public static bool IsAllowedArchiveExtension(string path)
    {
        return OptiClick.Infrastructure.Install.Components.InstallerExecutionHelpers.IsAllowedArchiveExtension(path);
    }

    public static void EnsureWritableIfExists(IInstallFileSystem fileSystem, string path)
    {
        if (fileSystem.FileExists(path) && !fileSystem.IsWritable(path))
        {
            fileSystem.SetWritable(path);
        }
    }
}

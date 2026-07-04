using System.IO;
using System.Linq;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Services;

public interface IAppUpdateService
{
    bool TryResolveUpdate(RemoteRuntimeData runtimeData, string currentVersion, out AppUpdateInfo? updateInfo);
}

public sealed class AppUpdateService(IAppUpdateVersionComparer versionComparer) : IAppUpdateService
{
    public bool TryResolveUpdate(RemoteRuntimeData runtimeData, string currentVersion, out AppUpdateInfo? updateInfo)
    {
        updateInfo = null;
        if (runtimeData is null || runtimeData.ResourceMaster.Count == 0)
        {
            return false;
        }

        var row = runtimeData.ResourceMaster.FirstOrDefault(IsOptiClickInstallerRow);
        if (row is null)
        {
            return false;
        }

        var latestVersion = RuntimeDataRowReader.GetString(row, "version");
        var url = RuntimeDataRowReader.GetFirstString(row, "url", "download_url", "source_url");
        if (string.IsNullOrWhiteSpace(url))
        {
            url = RuntimeDataRowReader.GetString(row, "link");
        }

        var filename = RuntimeDataRowReader.GetString(row, "filename");
        if (string.IsNullOrWhiteSpace(filename) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            filename = Path.GetFileName(uri.LocalPath);
        }

        if (string.IsNullOrWhiteSpace(latestVersion)
            || string.IsNullOrWhiteSpace(url)
            || string.IsNullOrWhiteSpace(filename))
        {
            return false;
        }

        var packageType = ResolvePackageType(filename, url);
        if (packageType == AppUpdatePackageType.Unknown)
        {
            return false;
        }

        var isForced = RuntimeDataRowReader.GetString(row, "force_update")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        if (!versionComparer.IsUpdateAvailable(currentVersion, latestVersion))
        {
            return false;
        }

        var displayVersion = RuntimeDataRowReader.GetString(row, "display_version");
        if (string.IsNullOrWhiteSpace(displayVersion))
        {
            displayVersion = latestVersion;
        }

        updateInfo = new AppUpdateInfo(
            currentVersion,
            latestVersion,
            displayVersion,
            url,
            filename,
            RuntimeDataRowReader.GetString(row, "note"),
            packageType,
            isForced,
            RuntimeDataRowReader.GetString(row, "sha256"));
        return true;
    }

    private static bool IsOptiClickInstallerRow(RuntimeDataResourceRow row)
    {
        var name = RuntimeDataRowReader.GetString(row, "name");
        return string.Equals(name, "OptiClick", StringComparison.OrdinalIgnoreCase);
    }

    private static AppUpdatePackageType ResolvePackageType(string filename, string url)
    {
        var extension = Path.GetExtension(filename);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(url);
        }

        return extension.ToLowerInvariant() switch
        {
            ".exe" => AppUpdatePackageType.Exe,
            ".zip" => AppUpdatePackageType.Zip,
            ".7z" => AppUpdatePackageType.SevenZip,
            _ => AppUpdatePackageType.Unknown
        };
    }
}

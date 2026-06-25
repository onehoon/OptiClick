namespace OptiClick.Core.Install;

public static class OptiScalerInstallLayout
{
    public const string RootDllFileName = "OptiScaler.dll";
    public const string RootIniFileName = "OptiScaler.ini";
    public const string LibraryDirectory = "OptiScaler";
    public const string PluginsDirectory = "OptiScaler/plugins";
    public const string OptiPatcherFileName = "OptiPatcher.asi";
    public const string PluginsToken = "plugins";

    public static bool IsPluginsToken(string? value)
    {
        var normalized = NormalizeRelativePath(value);
        return string.Equals(normalized, PluginsToken, StringComparison.OrdinalIgnoreCase);
    }

    public static string PluginFile(string fileName)
    {
        var normalized = NormalizeRelativePath(fileName);
        return string.IsNullOrWhiteSpace(normalized)
            ? PluginsDirectory
            : $"{PluginsDirectory}/{normalized}";
    }

    public static string NormalizeRelativePath(string? value)
    {
        var normalized = (value ?? "").Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized.Trim('/');
    }
}

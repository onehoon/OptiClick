namespace OptiClick.Infrastructure.Windows;

public sealed class Windows11OnlyOperatingSystemSupportPolicy : IOperatingSystemSupportPolicy
{
    public OperatingSystemSupportState Evaluate()
    {
        var version = Environment.OSVersion.Version;
        var versionText = $"{version.Major}.{version.Minor}.{version.Build}";

        if (!OperatingSystem.IsWindows())
        {
            return OperatingSystemSupportState.Supported(versionText);
        }

        // Windows 11 also reports major version 10, but uses build >= 22000.
        if (version.Major < 10 || (version.Major == 10 && version.Build < 22000))
        {
            return OperatingSystemSupportState.UnsupportedOperatingSystem(versionText);
        }

        return OperatingSystemSupportState.Supported(versionText);
    }
}

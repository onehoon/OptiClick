namespace OptiClick.Infrastructure.Windows;

public sealed record OperatingSystemSupportState
{
    public bool IsSupported { get; init; }
    public bool IsUnsupportedOperatingSystem => !IsSupported;
    public string VersionText { get; init; } = "";

    public static OperatingSystemSupportState Supported(string versionText) =>
        new()
        {
            IsSupported = true,
            VersionText = versionText
        };

    public static OperatingSystemSupportState UnsupportedOperatingSystem(string versionText) =>
        new()
        {
            IsSupported = false,
            VersionText = versionText
        };
}

namespace OptiClick.Infrastructure.Windows;

public sealed record OperatingSystemSupportState
{
    public bool IsSupported { get; init; }
    public bool IsUnsupportedWindows10 { get; init; }
    public bool IsWindows10 => IsUnsupportedWindows10;
    public string VersionText { get; init; } = "";

    public static OperatingSystemSupportState Supported(string versionText) =>
        new()
        {
            IsSupported = true,
            IsUnsupportedWindows10 = false,
            VersionText = versionText
        };

    public static OperatingSystemSupportState UnsupportedWindows10(string versionText) =>
        new()
        {
            IsSupported = false,
            IsUnsupportedWindows10 = true,
            VersionText = versionText
        };
}

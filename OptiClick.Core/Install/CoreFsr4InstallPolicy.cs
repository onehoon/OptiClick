namespace OptiClick.Core.Install;

public sealed class CoreFsr4InstallPolicy
{
    public bool ShouldInstall(bool manifestEnabled, string variant)
    {
        return manifestEnabled && !string.IsNullOrWhiteSpace(NormalizeVariant(variant));
    }

    public static string NormalizeVariant(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }
}

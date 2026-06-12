namespace OptiClick.Infrastructure.Install.Config;

internal static class ConfigProfileErrorKeyBuilder
{
    public static string Build(ConfigProfileError error)
    {
        return string.Join(
            "\u001F",
            error.ProfileName ?? "",
            error.ReasonCode ?? "",
            error.TargetPath ?? "",
            error.TargetKey ?? "",
            error.ValuePath ?? "",
            error.Detail ?? "",
            error.OldValue ?? "",
            error.NewValue ?? "");
    }
}

namespace OptiClick.Wpf.Services;

public static class AppUpdateStartupArguments
{
    public const string ForegroundAfterUpdate = "--foreground-after-update";

    public static bool RequestsForegroundAfterUpdate(IEnumerable<string> args)
    {
        return (args ?? []).Any(arg => string.Equals(arg, ForegroundAfterUpdate, StringComparison.OrdinalIgnoreCase));
    }
}

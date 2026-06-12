using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;

internal static class MainShellInteractionContextUtilities
{
    public static string NormalizeAppVersion(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "0.0.0" : normalized;
    }

    public static void ApplyAppLog(
        IAppLogger appLogger,
        bool shouldWrite,
        bool asWarning,
        string? category,
        string? message)
    {
        if (!shouldWrite || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var normalizedCategory = NormalizeStatusCode(category, MainViewModelLogCategories.App);
        var normalizedMessage = message.Trim();
        if (asWarning)
        {
            appLogger.Warning(normalizedCategory, normalizedMessage);
            return;
        }

        appLogger.Info(normalizedCategory, normalizedMessage);
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

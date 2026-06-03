namespace OptiClick.Wpf.Logging;

public static class AppLogSanitizer
{
    public static string Sanitize(string? value)
    {
        return OptiClick.Infrastructure.Logging.AppLogSanitizer.Sanitize(value);
    }
}

using System.Globalization;

namespace OptiClick.Wpf.Localization;

public static class LocalizedTextFormatter
{
    public static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }
}

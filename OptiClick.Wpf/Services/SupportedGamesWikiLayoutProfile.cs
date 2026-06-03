using System.Windows;

namespace OptiClick.Wpf.Services;

public static class SupportedGamesWikiLayoutProfile
{
    private const double CoverAspectWidthPerHeight = 2.0 / 3.0;
    public const string WikiThumbWidthResourceKey = "SupportedGamesWikiThumbWidthDip";
    public const string WikiThumbHeightResourceKey = "SupportedGamesWikiThumbHeightDip";
    public const string RowHeightResourceKey = "SupportedGamesWikiRowHeightDip";

    public const double DefaultRowHeightDip = 80;
    public const double DefaultWikiThumbHeightDip = DefaultRowHeightDip;
    public const double DefaultWikiThumbWidthDip = DefaultWikiThumbHeightDip * CoverAspectWidthPerHeight;
    private const double WikiRowCoverHeightBucketDip = 8.0;
    public const double MainCardWidthDip = 180;
    public const double MainCardHeightDip = 270;

    public static double ResolveWikiThumbWidthDip()
    {
        return ResolveMetric(WikiThumbWidthResourceKey, DefaultWikiThumbWidthDip);
    }

    public static double ResolveWikiThumbHeightDip()
    {
        return ResolveMetric(WikiThumbHeightResourceKey, DefaultWikiThumbHeightDip);
    }

    public static double ResolveRowHeightDip()
    {
        return ResolveMetric(RowHeightResourceKey, DefaultRowHeightDip);
    }

    public static double ResolveMainCardWidthDip()
    {
        return MainCardWidthDip;
    }

    public static double ResolveMainCardHeightDip()
    {
        return MainCardHeightDip;
    }

    public static double ResolveWikiRowCoverHeightDip()
    {
        var rowHeight = ResolveRowHeightDip();
        return NormalizeWikiRowHeightDip(rowHeight);
    }

    public static double ResolveWikiRowCoverWidthDip()
    {
        var rowHeight = ResolveWikiRowCoverHeightDip();
        return Math.Round(rowHeight * CoverAspectWidthPerHeight, 2);
    }

    private static double ResolveMetric(string resourceKey, double defaultValue)
    {
        if (TryResolveDouble(Application.Current?.MainWindow, resourceKey, out var value))
        {
            return value;
        }

        if (TryResolveDouble(Application.Current?.Resources, resourceKey, out value))
        {
            return value;
        }

        return defaultValue;
    }

    private static double NormalizeWikiRowHeightDip(double rowHeightDip)
    {
        if (rowHeightDip <= 0)
        {
            return DefaultRowHeightDip;
        }

        return Math.Round(rowHeightDip / WikiRowCoverHeightBucketDip, MidpointRounding.AwayFromZero) * WikiRowCoverHeightBucketDip;
    }

    private static bool TryResolveDouble(DependencyObject? source, string key, out double value)
    {
        value = 0;
        if (source is null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (source is not FrameworkElement frameworkElement)
        {
            return false;
        }

        try
        {
            var dispatcher = frameworkElement.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                var result = dispatcher.Invoke(() =>
                {
                    if (TryResolveDoubleFromFrameworkElementResources(frameworkElement, key, out var resolvedValue))
                    {
                        return (found: true, resolvedValue);
                    }

                    return (found: false, resolvedValue: 0d);
                });

                value = result.resolvedValue;
                return result.found;
            }

            return TryResolveDoubleFromFrameworkElementResources(frameworkElement, key, out value);
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private static bool TryResolveDouble(ResourceDictionary? resources, string key, out double value)
    {
        value = 0;
        if (resources is null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (!resources.Contains(key))
        {
            return false;
        }

        if (resources[key] is not double direct)
        {
            return false;
        }

        value = direct;
        return true;
    }

    private static bool TryResolveDoubleFromFrameworkElementResources(FrameworkElement frameworkElement, string key, out double value)
    {
        value = 0;
        if (!frameworkElement.Resources.Contains(key))
        {
            return false;
        }

        if (frameworkElement.Resources[key] is not double direct)
        {
            return false;
        }

        value = direct;
        return true;
    }
}

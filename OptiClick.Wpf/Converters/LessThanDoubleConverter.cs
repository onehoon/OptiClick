using System;
using System.Globalization;
using System.Windows.Data;

namespace OptiClick.Wpf.Converters;

public sealed class LessThanDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!TryReadDouble(value, out var actual))
        {
            return false;
        }

        if (!TryReadDouble(parameter, out var threshold))
        {
            return false;
        }

        return actual < threshold;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    private static bool TryReadDouble(object? value, out double parsed)
    {
        switch (value)
        {
            case double doubleValue:
                parsed = doubleValue;
                return true;
            case float floatValue:
                parsed = floatValue;
                return true;
            case int intValue:
                parsed = intValue;
                return true;
            case long longValue:
                parsed = longValue;
                return true;
            case string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var fromText):
                parsed = fromText;
                return true;
            default:
                parsed = 0;
                return false;
        }
    }
}

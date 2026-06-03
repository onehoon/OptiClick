using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.Converters;

public sealed class ShellViewKindToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ShellViewKind currentView || parameter is not string parameterText)
        {
            return Visibility.Collapsed;
        }

        if (!Enum.TryParse<ShellViewKind>(parameterText, true, out var expectedView))
        {
            return Visibility.Collapsed;
        }

        return currentView == expectedView ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

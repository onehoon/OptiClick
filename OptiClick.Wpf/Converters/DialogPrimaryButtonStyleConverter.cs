using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Converters;

public sealed class DialogPrimaryButtonStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Application.Current is null)
        {
            return Binding.DoNothing;
        }

        var dialog = value as DialogRequestViewModel;
        var styleKey = ResolveStyleKey(dialog);

        if (Application.Current.TryFindResource(styleKey) is Style style)
        {
            return style;
        }

        return Application.Current.TryFindResource("PrimaryButtonStyle") as Style ?? Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    private static string ResolveStyleKey(DialogRequestViewModel? dialog)
    {
        if (dialog is null)
        {
            return "PrimaryButtonStyle";
        }

        if (dialog.Request.PrimaryButtonRole == DialogButtonRole.Destructive)
        {
            return "DangerButtonStyle";
        }

        if (dialog.Request.PrimaryButtonRole == DialogButtonRole.Success
            || dialog.Request.Severity == DialogSeverity.Success)
        {
            return "SuccessButtonStyle";
        }

        return "PrimaryButtonStyle";
    }
}

using System.Windows;
using System.Windows.Controls;
using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Views.Sections;

public partial class SettingsSectionView : UserControl
{
    public SettingsSectionView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshThemeToggleAppearance(ThemeService.Current.CurrentTheme);
        ThemeService.Current.ThemeChanged += OnThemeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.Current.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        Dispatcher.Invoke(() => RefreshThemeToggleAppearance(theme));
    }

    private void DarkThemeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeService.Current.SetTheme(AppTheme.Dark);
    }

    private void LightThemeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeService.Current.SetTheme(AppTheme.Light);
    }

    private void RefreshThemeToggleAppearance(AppTheme theme)
    {
        DarkThemeButton.Style = (Style)FindResource(theme == AppTheme.Dark
            ? "SettingsThemeToggleButtonSelectedStyle"
            : "SettingsThemeToggleButtonStyle");
        LightThemeButton.Style = (Style)FindResource(theme == AppTheme.Light
            ? "SettingsThemeToggleButtonSelectedStyle"
            : "SettingsThemeToggleButtonStyle");
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using Wpf.Ui.Appearance;

namespace OptiClick.Wpf.Services;

public sealed class ThemeService : IThemeService
{
    private static IThemeService? _current;

    private readonly IAppUserSettingsStore _userSettingsStore;

    private ThemeService(IAppUserSettingsStore userSettingsStore)
    {
        _userSettingsStore = userSettingsStore;
        CurrentTheme = ParseTheme(_userSettingsStore.Load().ThemePreference);
        Apply(CurrentTheme);
    }

    public static IThemeService Current =>
        _current ?? throw new InvalidOperationException("ThemeService has not been initialized.");

    public static IThemeService Initialize(IAppUserSettingsStore userSettingsStore)
    {
        ArgumentNullException.ThrowIfNull(userSettingsStore);
        var service = new ThemeService(userSettingsStore);
        _current = service;
        return service;
    }

    public AppTheme CurrentTheme { get; private set; }

    public event Action<AppTheme>? ThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (theme == CurrentTheme)
        {
            return;
        }

        CurrentTheme = theme;
        Apply(theme);
        Persist(theme);
        ThemeChanged?.Invoke(theme);
    }

    private void Persist(AppTheme theme)
    {
        var current = _userSettingsStore.Load();
        _userSettingsStore.Save(current with { ThemePreference = theme.ToString() });
    }

    private static AppTheme ParseTheme(string? value)
    {
        return string.Equals(value, nameof(AppTheme.Light), StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Light
            : AppTheme.Dark;
    }

    private static void Apply(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var merged = app.Resources.MergedDictionaries;
        var tokensUri = new Uri(theme == AppTheme.Dark
            ? "Resources/Theme.Tokens.Dark.xaml"
            : "Resources/Theme.Tokens.Light.xaml", UriKind.Relative);

        ReplaceDictionary(merged, tokensUri, "AppBackgroundColor");

        // Brushes bind Color via DynamicResource to the token dictionary above, but a shared
        // Freezable (like a Brush) loses its live resource-update tracking once it's referenced
        // by more than one element (WPF sets its InheritanceContext to null). Re-parsing the
        // brushes dictionary forces brand-new brush instances that resolve the just-swapped
        // token colors fresh, instead of relying on the stale live binding.
        var brushesUri = new Uri("Resources/Theme.Brushes.xaml", UriKind.Relative);
        ReplaceDictionary(merged, brushesUri, "AppBackgroundBrush");

        ApplicationThemeManager.Apply(
            theme == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
            updateAccent: false);
    }

    private static void ReplaceDictionary(Collection<ResourceDictionary> merged, Uri source, string markerKey)
    {
        var replacement = new ResourceDictionary { Source = source };
        var existing = merged.FirstOrDefault(d => d.Contains(markerKey));
        if (existing is not null)
        {
            merged[merged.IndexOf(existing)] = replacement;
        }
        else
        {
            merged.Add(replacement);
        }
    }
}

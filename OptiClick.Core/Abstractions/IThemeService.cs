namespace OptiClick.Core.Abstractions;

public enum AppTheme
{
    Dark,
    Light
}

public interface IThemeService
{
    AppTheme CurrentTheme { get; }

    void SetTheme(AppTheme theme);

    event Action<AppTheme>? ThemeChanged;
}

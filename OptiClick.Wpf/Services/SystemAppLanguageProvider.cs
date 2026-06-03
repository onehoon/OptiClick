using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;

namespace OptiClick.Wpf.Services;

public sealed class SystemAppLanguageProvider : IWritableAppLanguageProvider
{
    private const string EnvironmentVariableName = "OPTICLICK_UI_LANGUAGE";
    private readonly IAppLogger _logger;
    private readonly Func<string, string?> _environmentReader;
    private readonly Func<CultureInfo> _uiCultureReader;
    private readonly Func<CultureInfo> _cultureReader;

    public SystemAppLanguageProvider(IAppLogger? logger = null)
        : this(
            logger ?? NullAppLogger.Instance,
            static name => Environment.GetEnvironmentVariable(name),
            static () => CultureInfo.CurrentUICulture,
            static () => CultureInfo.CurrentCulture)
    {
    }

    public SystemAppLanguageProvider(
        IAppLogger logger,
        Func<string, string?> environmentReader,
        Func<CultureInfo> uiCultureReader,
        Func<CultureInfo> cultureReader)
    {
        _logger = logger ?? NullAppLogger.Instance;
        _environmentReader = environmentReader ?? throw new ArgumentNullException(nameof(environmentReader));
        _uiCultureReader = uiCultureReader ?? throw new ArgumentNullException(nameof(uiCultureReader));
        _cultureReader = cultureReader ?? throw new ArgumentNullException(nameof(cultureReader));
        CurrentLanguage = ResolveInitialLanguage();
    }

    public IReadOnlyList<AppLanguage> SupportedLanguages { get; } = [AppLanguage.English, AppLanguage.Korean];

    public AppLanguage CurrentLanguage { get; private set; }

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = language;
    }

    private AppLanguage ResolveInitialLanguage()
    {
        var rawValue = (_environmentReader(EnvironmentVariableName) ?? "").Trim();
        if (TryResolveFromEnvironment(rawValue, out var forcedLanguage))
        {
            return forcedLanguage;
        }

        if (!string.IsNullOrWhiteSpace(rawValue)
            && !string.Equals(rawValue, "auto", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(rawValue, "0", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("i18n", $"ui_language_env_invalid value={rawValue}");
        }

        var uiCulture = _uiCultureReader();
        if (TryResolveFromCulture(uiCulture, out var uiLanguage))
        {
            return uiLanguage;
        }

        var culture = _cultureReader();
        if (TryResolveFromCulture(culture, out var cultureLanguage))
        {
            return cultureLanguage;
        }

        return AppLanguage.English;
    }

    private static bool TryResolveFromEnvironment(string rawValue, out AppLanguage language)
    {
        var normalized = (rawValue ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized)
            || string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase))
        {
            language = AppLanguage.English;
            return false;
        }

        switch (normalized)
        {
            case "ko":
            case "kr":
            case "korean":
                language = AppLanguage.Korean;
                return true;
            case "en":
            case "english":
                language = AppLanguage.English;
                return true;
            default:
                language = AppLanguage.English;
                return false;
        }
    }

    private static bool TryResolveFromCulture(CultureInfo? culture, out AppLanguage language)
    {
        var cultureName = (culture?.Name ?? "").Trim();
        if (cultureName.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
        {
            language = AppLanguage.Korean;
            return true;
        }

        if (cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            language = AppLanguage.English;
            return true;
        }

        language = AppLanguage.English;
        return false;
    }

}

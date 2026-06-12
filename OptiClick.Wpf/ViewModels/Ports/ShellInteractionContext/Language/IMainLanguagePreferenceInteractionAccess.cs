using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.Language;

internal interface IMainLanguagePreferenceInteractionAccess
{
    AppLanguage SelectedLanguage { get; }
    string NormalizeLanguagePreference(string? preference);
    AppLanguage ResolvePreferredLanguage(string languagePreference);
    string ResolveLanguageOptionFromState(string preference);
    void ApplyChangedLanguage(AppLanguage preferredLanguage);
    void ApplyLoadedSettings(string languageOption);
}

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Localization;

namespace OptiClick.Wpf.ViewModels.Ports.Localization;

internal interface IMainShellLocalizationPortAccess
{
    AppLanguage SelectedLanguage { get; }
    string LanguagePreference { get; }
    void RefreshLocalizedStrings();
    void ApplyLocalizationStateUpdate(LocalizationStateUpdate update);
    void ApplySettingsLanguageOption(string option);
}

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

internal interface IMainUserSettingsInteractionAccess
{
    string LanguagePreference { set; }
    string PreferredOptiScalerVariant { set; }
}

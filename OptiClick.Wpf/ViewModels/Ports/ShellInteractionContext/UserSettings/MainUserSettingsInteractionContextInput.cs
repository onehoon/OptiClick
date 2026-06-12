using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.UserSettings;

internal sealed record MainUserSettingsInteractionContextInput
{
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Func<string?, string> NormalizeLanguagePreference { get; init; }
    public required Func<string?, string> NormalizeOptiScalerVariantPreference { get; init; }
    public required Func<string, AppLanguage> ResolvePreferredLanguage { get; init; }
    public required Func<string, string> ResolveLanguageOptionFromState { get; init; }
    public required Action<string> SetLanguagePreference { get; init; }
    public required Action<string> SetOptiScalerVariantPreference { get; init; }
    public required Action<AppLanguage> ApplyChangedLanguage { get; init; }
    public required Action<string> ApplyLoadedSettings { get; init; }
    public required Action<string, OptiScalerCommonIniSettingsDocument?> ApplySavedOptiScalerSettings { get; init; }
    public required Func<OptiScalerCommonIniSettingsDocument?> LoadCommonIniSettings { get; init; }
}

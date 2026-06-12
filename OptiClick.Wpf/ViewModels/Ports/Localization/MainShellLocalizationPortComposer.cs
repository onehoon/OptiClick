using OptiClick.Wpf.ViewModels.Features.Shell;

namespace OptiClick.Wpf.ViewModels.Ports.Localization;

internal sealed record MainShellLocalizationPortCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required IMainShellLocalizationPortAccess Access { get; init; }
    public required Func<MainShellInteractionFeatureFacade> ResolveShellInteractionFeature { get; init; }
}

internal static class MainShellLocalizationPortComposer
{
    public static MainShellFacadeLocalizationPort Compose(MainShellLocalizationPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeLocalizationPort
        {
            ReadSelectedLanguage = () => access.SelectedLanguage,
            ReadLanguagePreference = () => access.LanguagePreference,
            SetLanguage = input.AppDependencies.LanguageProvider.SetLanguage,
            RefreshLocalizedStrings = access.RefreshLocalizedStrings,
            BuildRefreshState = (language, strings) =>
                input.ResolveShellInteractionFeature().BuildRefreshLocalizationState(language, strings),
            ApplyLocalizationStateUpdate = access.ApplyLocalizationStateUpdate,
            ApplySettingsLanguageOption = access.ApplySettingsLanguageOption
        };
    }
}

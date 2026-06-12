using OptiClick.Wpf.ViewModels.Features.Install;
using OptiClick.Wpf.ViewModels.Features.Runtime;
using OptiClick.Wpf.ViewModels.Features.Shell;

namespace OptiClick.Wpf.ViewModels.Ports.Install;

internal sealed record MainShellInstallPortCompositionInput
{
    public required IMainShellInstallPortAccess Access { get; init; }
    public required Func<MainShellInteractionFeatureFacade> ResolveShellInteractionFeature { get; init; }
    public required Func<MainInstallFeatureFacade> ResolveInstallFeature { get; init; }
    public required Func<MainRuntimeFeatureFacade> ResolveRuntimeFeature { get; init; }
}

internal static class MainShellInstallPortComposer
{
    public static MainShellFacadeInstallPort Compose(MainShellInstallPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeInstallPort
        {
            ReadInstallButtonText = () => access.InstallButtonText,
            ApplyInstallBusyState = (inProgress, message, restoreSelectionState) =>
                access.ApplyBusyStateUpdate(
                    input.ResolveShellInteractionFeature().CreateInstallBusyState(
                        inProgress,
                        access.IsAppUpdateInProgress,
                        access.SelectionState,
                        restoreSelectionState,
                        message)),
            ShowInstallAsync = cancellationToken =>
                input.ResolveInstallFeature().ShowInstallDialogAsync(cancellationToken),
            HandleUninstallAsync = (game, cancellationToken) =>
                input.ResolveInstallFeature().HandleUninstallAsync(game, cancellationToken),
            ExecuteCurrentInstallFlowAsync = cancellationToken =>
                input.ResolveInstallFeature().ExecuteCurrentInstallFlowAsync(cancellationToken),
            ClearSelectedGameContext = access.ClearSelectedGameContext,
            ReadPreferredOptiScalerVariant = () => access.PreferredOptiScalerVariant,
            ApplyOptiScalerVariantOptions = access.ApplyOptiScalerVariantOptions,
            PersistEffectiveVariantPreference = variant =>
                access.PreferredOptiScalerVariant = access.NormalizeOptiScalerVariantPreference(variant),
            SetOptiScalerVariantPreference = value => access.PreferredOptiScalerVariant = value,
            SaveUserSettings = () => input.ResolveShellInteractionFeature().SavePreferencesNonBlocking(
                access.LanguagePreference,
                access.PreferredOptiScalerVariant),
            IsOperatingSystemSupported = () => input.ResolveRuntimeFeature().IsOperatingSystemPolicySupported(),
            RefreshArchiveReadinessAsync =
                cancellationToken => input.ResolveInstallFeature().RefreshArchiveReadinessAsync(cancellationToken),
            RefreshArchiveReadinessWithoutCoordinatorAsync =
                cancellationToken => input.ResolveInstallFeature()
                    .RefreshArchiveReadinessWithoutCoordinatorAsync(cancellationToken)
        };
    }
}

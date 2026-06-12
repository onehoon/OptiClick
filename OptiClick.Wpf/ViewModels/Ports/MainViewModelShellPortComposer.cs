using OptiClick.Wpf.ViewModels.Ports.App;
using OptiClick.Wpf.ViewModels.Ports.Install;
using OptiClick.Wpf.ViewModels.Ports.Localization;
using OptiClick.Wpf.ViewModels.Ports.Runtime;
using OptiClick.Wpf.ViewModels.Ports.Selection;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;
using OptiClick.Wpf.ViewModels.Ports.Startup;
using OptiClick.Wpf.ViewModels.Ports.Ui;

namespace OptiClick.Wpf.ViewModels.Ports;

internal sealed record MainViewModelShellPortGraph
{
    public required MainShellFacadePorts FacadePorts { get; init; }
    public required MainShellInteractionContextFactoryInput ShellInteractionContext { get; init; }
}

internal sealed record MainViewModelShellPortCompositionInput
{
    public required MainViewModelCompositionDependencies Dependencies { get; init; }
    public required MainViewModelFeatureFacadeRegistry FeatureRegistry { get; init; }
    public required MainShellPortAccessGraph Access { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
}

internal static class MainViewModelShellPortComposer
{
    public static MainViewModelShellPortGraph Compose(MainViewModelShellPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainViewModelShellPortGraph
        {
            FacadePorts = ComposeFacadePorts(input),
            ShellInteractionContext = MainShellInteractionContextComposer.Compose(
                new MainShellInteractionContextCompositionInput
                {
                    Dependencies = input.Dependencies,
                    Accesses = input.Access.ShellInteractionContext,
                    OptiScalerSettingsController = input.OptiScalerSettingsController
                })
        };
    }

    private static MainShellFacadePorts ComposeFacadePorts(MainViewModelShellPortCompositionInput input)
    {
        var dependencies = input.Dependencies;
        var featureRegistry = input.FeatureRegistry;

        return new MainShellFacadePorts
        {
            App = MainShellAppPortComposer.Compose(
                new MainShellAppPortCompositionInput
                {
                    AppDependencies = dependencies.App,
                    ShellDependencies = dependencies.Shell,
                    StartupDependencies = dependencies.Features.Startup,
                    Access = input.Access.App,
                    ResolveRuntimeFeature = () => featureRegistry.RuntimeFeature
                }),
            Runtime = MainShellRuntimePortComposer.Compose(
                new MainShellRuntimePortCompositionInput
                {
                    Access = input.Access.Runtime,
                    ResolveRuntimeFeature = () => featureRegistry.RuntimeFeature
                }),
            Startup = MainShellStartupPortComposer.Compose(
                new MainShellStartupPortCompositionInput
                {
                    Access = input.Access.Startup,
                    ResolveShellInteractionFeature = () => featureRegistry.ShellInteractionFeature,
                    ResolveStartupFeature = () => featureRegistry.StartupFeature
                }),
            Selection = MainShellSelectionPortComposer.Compose(
                new MainShellSelectionPortCompositionInput
                {
                    Access = input.Access.Selection,
                    ResolveSelectionFeature = () => featureRegistry.SelectionFeature
                }),
            Install = MainShellInstallPortComposer.Compose(
                new MainShellInstallPortCompositionInput
                {
                    Access = input.Access.Install,
                    ResolveShellInteractionFeature = () => featureRegistry.ShellInteractionFeature,
                    ResolveInstallFeature = () => featureRegistry.InstallFeature,
                    ResolveRuntimeFeature = () => featureRegistry.RuntimeFeature
                }),
            Ui = MainShellUiPortComposer.Compose(
                new MainShellUiPortCompositionInput
                {
                    Access = input.Access.Ui,
                    ResolveShellInteractionFeature = () => featureRegistry.ShellInteractionFeature
                }),
            Localization = MainShellLocalizationPortComposer.Compose(
                new MainShellLocalizationPortCompositionInput
                {
                    AppDependencies = dependencies.App,
                    Access = input.Access.Localization,
                    ResolveShellInteractionFeature = () => featureRegistry.ShellInteractionFeature
                })
        };
    }
}

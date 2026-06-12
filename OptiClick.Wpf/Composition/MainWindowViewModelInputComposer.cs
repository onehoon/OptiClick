using OptiClick.Wpf.Composition.Features;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.Composition;

internal sealed record MainWindowViewModelInputComposerRequest
{
    public required AppSharedServices App { get; init; }
    public required RuntimeCompositionServices Runtime { get; init; }
    public required ScanCompositionServices Scan { get; init; }
    public required InstallCompositionServices Install { get; init; }
    public required UpdateCompositionServices Update { get; init; }
    public required SupportCompositionServices Support { get; init; }
    public bool SeedMockGameCards { get; init; }
    public bool SeedMockScanFolders { get; init; }
}

internal static class MainWindowViewModelInputComposer
{
    public static MainViewModelResolvedFactoryInput Compose(MainWindowViewModelInputComposerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var app = request.App;
        var runtime = request.Runtime;
        var scan = request.Scan;
        var install = request.Install;
        var update = request.Update;
        var support = request.Support;

        var startupNoticePresenter = new StartupNoticePresenter();
        var startupComposition = MainWindowStartupCompositionFactory.Create(
            app,
            startupNoticePresenter);

        var scanDependencies = MainWindowScanFeatureComposer.Compose(scan);
        var shellComposition = MainWindowShellFeatureComposer.Compose(
            app,
            install,
            startupNoticePresenter,
            startupComposition,
            support);
        var runtimeDependencies = MainWindowRuntimeFeatureComposer.Compose(
            runtime,
            shellComposition.Shell.FlowLogDispatcher);
        var installDependencies = MainWindowInstallFeatureComposer.Compose(
            install,
            shellComposition.Shell.DialogPresenter,
            app.AppLogger,
            app);
        var startupDependencies = MainWindowStartupFeatureComposer.Compose(
            startupNoticePresenter,
            startupComposition,
            shellComposition.SelectionServices);
        var selectionDependencies = MainWindowSelectionFeatureComposer.Compose(
            install,
            shellComposition.SelectionServices);

        return new MainViewModelResolvedFactoryInput
        {
            Dependencies = new MainViewModelCompositionDependencies
            {
                App = MainWindowAppFeatureComposer.Compose(app, update),
                Shell = shellComposition.Shell,
                Features = new MainFeatureResolvedDependencies
                {
                    Runtime = runtimeDependencies,
                    Scan = scanDependencies,
                    Install = installDependencies,
                    Startup = startupDependencies,
                    Selection = selectionDependencies,
                    Support = MainWindowSupportFeatureComposer.Compose(support),
                    Update = MainWindowUpdateFeatureComposer.Compose(update),
                    ShellSections = MainWindowShellSectionsFeatureComposer.Compose(
                        app,
                        shellComposition.Shell,
                        startupComposition),
                    RuntimeFlow = new MainRuntimeFlowResolvedDependencies
                    {
                        RuntimeContextCoordinator = runtimeDependencies.RuntimeContextCoordinator,
                        DeviceIdentityRulesFlowController = runtimeDependencies.DeviceIdentityRulesFlowController,
                        GpuSelectionCoordinator = runtimeDependencies.GpuSelectionCoordinator
                    },
                    SelectionScan = new MainSelectionScanResolvedDependencies
                    {
                        ShellGameCardViewModelFactory = runtimeDependencies.ShellGameCardViewModelFactory,
                        ScanVisibleGameResolver = scanDependencies.ScanVisibleGameResolver,
                        GameSelectionFlowController = selectionDependencies.GameSelectionFlowController,
                        SelectionPopupCoordinator = selectionDependencies.SelectionPopupCoordinator,
                        GameCardSelectionStateController = selectionDependencies.GameCardSelectionStateController,
                        GpuSelectionCoordinator = runtimeDependencies.GpuSelectionCoordinator
                    }
                }
            },
            SeedMockGameCards = request.SeedMockGameCards,
            SeedMockScanFolders = request.SeedMockScanFolders
        };
    }
}

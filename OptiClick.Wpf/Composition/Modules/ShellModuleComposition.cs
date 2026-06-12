using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record ShellUiServices
{
    public required ShellNavigationState NavigationState { get; init; }
    public required ShellChromeViewModels ShellChrome { get; init; }
    public required UserSettingsController UserSettingsController { get; init; }
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required LocalizationStateController LocalizationStateController { get; init; }
    public required MainViewModelBusyStateApplier BusyStateApplier { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required MainShellInteractionControllers InteractionControllers { get; init; }
}

internal sealed record ShellDialogServices
{
    public required DialogPresenter DialogPresenter { get; init; }
    public required InstallManagementDialogHostViewModel InstallManagementDialogHost { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required OnceDialogGate RemoteCatalogDialogGate { get; init; }
    public required DialogHostViewModel DialogHost { get; init; }
}

internal sealed record ShellSectionServices
{
    public required ShellSectionsFactory ShellSectionsFactory { get; init; }
    public required ShellSectionsCompositionFactory ShellSectionsCompositionFactory { get; init; }
}

internal sealed record ShellModuleCompositionServices
{
    public required ShellUiServices ShellUiServices { get; init; }
    public required ShellDialogServices ShellDialogServices { get; init; }
    public required ShellSelectionModuleCompositionServices ShellSelectionServices { get; init; }
    public required ShellSupportModuleCompositionServices ShellSupportServices { get; init; }
    public required ShellSectionServices ShellSectionServices { get; init; }
}

internal static class ShellModuleComposition
{
    public static ShellModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelRequiredDependencies requiredDependencies,
        MainViewModelAppFallbackServices fallbackServices,
        InstallDependencyComposition installComposition,
        StartupModuleCompositionServices startupComposition,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(requiredDependencies);
        ArgumentNullException.ThrowIfNull(fallbackServices);
        ArgumentNullException.ThrowIfNull(installComposition);
        ArgumentNullException.ThrowIfNull(startupComposition);
        ArgumentNullException.ThrowIfNull(appLogger);

        var shellDependencyBundles = MainViewModelShellDependencyNormalizer.Normalize(appDependencies);
        var shellUi = shellDependencyBundles.Ui;
        var shellDialogs = shellDependencyBundles.Dialogs;
        var shellSections = shellDependencyBundles.Sections;
        var flowLogDispatcher = shellUi.FlowLogDispatcher
                                ?? new FlowLogDispatcher(appLogger);
        var flowRequestFactory = shellUi.FlowRequestFactory
                                 ?? new MainViewModelFlowRequestFactory();
        var userSettingsStore = fallbackServices.UserSettingsStore;

        ShellNavigationState navigationState;
        ShellChromeViewModels shellChrome;
        var configuredShellChrome = shellUi.ShellChrome;
        var configuredNavigationState = shellUi.NavigationState;
        if (configuredShellChrome is null)
        {
            navigationState = configuredNavigationState ?? new ShellNavigationState();
            shellChrome = ShellChromeViewModels.Create(navigationState);
        }
        else
        {
            shellChrome = configuredShellChrome;
            navigationState = configuredNavigationState ?? shellChrome.NavigationState;
            EnsureShellChromeNavigationState(shellChrome, navigationState);
        }

        var dialogPresenter = shellDialogs.DialogPresenter
                              ?? new DialogPresenter(requiredDependencies.DialogService, appLogger);
        var remoteCatalogDialogGate = shellDialogs.RemoteCatalogDialogGate
                                      ?? new OnceDialogGate();
        var userSettingsController = shellUi.UserSettingsController
                                     ?? new UserSettingsController(userSettingsStore, appLogger);
        var supportedGamesWikiMarkdownLoader = shellUi.SupportedGamesWikiMarkdownLoader
                                               ?? new NoopSupportedGamesWikiMarkdownLoader();
        var supportComposition = ShellSupportModuleComposition.Compose(
            appDependencies,
            fallbackServices,
            startupComposition);
        var localizationStateController = shellUi.LocalizationStateController
                                          ?? new LocalizationStateController();
        var busyStateApplier = shellUi.BusyStateApplier
                               ?? new MainViewModelBusyStateApplier();
        var selectionComposition = ShellSelectionModuleComposition.Compose(
            appDependencies,
            installComposition,
            startupComposition,
            dialogPresenter,
            flowLogDispatcher,
            appLogger);
        var dialogHost = shellDialogs.DialogHost ?? new DialogHostViewModel();
        var installManagementDialogHost = shellDialogs.InstallManagementDialogHost
                                          ?? new InstallManagementDialogHostViewModel();
        var installManagementDialogService = shellDialogs.InstallManagementDialogService
                                             ?? new OverlayInstallManagementDialogService(installManagementDialogHost);
        var resultApplier = shellUi.ResultApplier
                            ?? new MainViewModelResultApplier();
        var interactionControllers = shellUi.ShellInteractionControllers
                                     ?? new MainShellInteractionControllers
        {
            OptiScalerDirtyNavigationGuard = new OptiScalerDirtyNavigationGuard(),
            AppUpdateInteractionController = new MainAppUpdateInteractionController(),
            UserSettingsApplyController = new MainUserSettingsApplyController()
        };
        var shellSectionsFactory = shellSections.ShellSectionsFactory
                                   ?? new ShellSectionsFactory();
        var shellSectionsCompositionFactory = shellSections.ShellSectionsCompositionFactory
                                              ?? new ShellSectionsCompositionFactory();

        return new ShellModuleCompositionServices
        {
            ShellUiServices = new ShellUiServices
            {
                NavigationState = navigationState,
                ShellChrome = shellChrome,
                UserSettingsController = userSettingsController,
                SupportedGamesWikiMarkdownLoader = supportedGamesWikiMarkdownLoader,
                LocalizationStateController = localizationStateController,
                BusyStateApplier = busyStateApplier,
                FlowLogDispatcher = flowLogDispatcher,
                FlowRequestFactory = flowRequestFactory,
                ResultApplier = resultApplier,
                InteractionControllers = interactionControllers
            },
            ShellDialogServices = new ShellDialogServices
            {
                DialogPresenter = dialogPresenter,
                InstallManagementDialogHost = installManagementDialogHost,
                InstallManagementDialogService = installManagementDialogService,
                RemoteCatalogDialogGate = remoteCatalogDialogGate,
                DialogHost = dialogHost
            },
            ShellSelectionServices = selectionComposition,
            ShellSupportServices = supportComposition,
            ShellSectionServices = new ShellSectionServices
            {
                ShellSectionsFactory = shellSectionsFactory,
                ShellSectionsCompositionFactory = shellSectionsCompositionFactory
            }
        };
    }

    private static void EnsureShellChromeNavigationState(
        ShellChromeViewModels shellChrome,
        ShellNavigationState navigationState)
    {
        if (ReferenceEquals(shellChrome.NavigationState, navigationState))
        {
            return;
        }

        throw new InvalidOperationException(
            "MainViewModel dependency 'ShellChrome.NavigationState' must reference the same instance as 'NavigationState'.");
    }
}

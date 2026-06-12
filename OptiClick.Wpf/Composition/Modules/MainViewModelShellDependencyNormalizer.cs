using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record MainViewModelShellDependencyBundles
{
    public required MainViewModelShellUiDependencies Ui { get; init; }
    public required MainViewModelShellDialogDependencies Dialogs { get; init; }
    public required MainViewModelShellSectionDependencies Sections { get; init; }
}

internal static class MainViewModelShellDependencyNormalizer
{
    public static MainViewModelShellDependencyBundles Normalize(MainViewModelAppDependencies appDependencies)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);

        var shellUi = appDependencies.ShellUi ?? new MainViewModelShellUiDependencies();
        var shellDialogs = appDependencies.ShellDialogs ?? new MainViewModelShellDialogDependencies();
        var shellSections = appDependencies.ShellSections ?? new MainViewModelShellSectionDependencies();

        return new MainViewModelShellDependencyBundles
        {
            Ui = shellUi with
            {
                NavigationState = shellUi.NavigationState ?? appDependencies.NavigationState,
                ShellChrome = shellUi.ShellChrome ?? appDependencies.ShellChrome,
                UserSettingsController = shellUi.UserSettingsController ?? appDependencies.UserSettingsController,
                SupportedGamesWikiMarkdownLoader =
                    shellUi.SupportedGamesWikiMarkdownLoader ?? appDependencies.SupportedGamesWikiMarkdownLoader,
                LocalizationStateController =
                    shellUi.LocalizationStateController ?? appDependencies.LocalizationStateController,
                BusyStateApplier = shellUi.BusyStateApplier ?? appDependencies.BusyStateApplier,
                FlowLogDispatcher = shellUi.FlowLogDispatcher ?? appDependencies.FlowLogDispatcher,
                FlowRequestFactory = shellUi.FlowRequestFactory ?? appDependencies.FlowRequestFactory,
                ResultApplier = shellUi.ResultApplier ?? appDependencies.ResultApplier,
                ShellInteractionControllers =
                    shellUi.ShellInteractionControllers ?? appDependencies.ShellInteractionControllers
            },
            Dialogs = shellDialogs with
            {
                DialogHost = shellDialogs.DialogHost ?? appDependencies.DialogHost,
                DialogPresenter = shellDialogs.DialogPresenter ?? appDependencies.DialogPresenter,
                RemoteCatalogDialogGate = shellDialogs.RemoteCatalogDialogGate ?? appDependencies.RemoteCatalogDialogGate,
                InstallManagementDialogHost =
                    shellDialogs.InstallManagementDialogHost ?? appDependencies.InstallManagementDialogHost,
                InstallManagementDialogService =
                    shellDialogs.InstallManagementDialogService ?? appDependencies.InstallManagementDialogService
            },
            Sections = shellSections with
            {
                ShellSectionsFactory = shellSections.ShellSectionsFactory ?? appDependencies.ShellSectionsFactory,
                ShellSectionsCompositionFactory =
                    shellSections.ShellSectionsCompositionFactory ?? appDependencies.ShellSectionsCompositionFactory
            }
        };
    }
}

using OptiClick.Wpf.Composition;
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

namespace OptiClick.Wpf.Composition.Features;

internal static class MainWindowSelectionFeatureComposer
{
    public static MainSelectionResolvedDependencies Compose(
        InstallCompositionServices install,
        ShellSelectionModuleCompositionServices selectionServices)
    {
        return new MainSelectionResolvedDependencies
        {
            GameSelectionFlowController = install.GameSelectionFlowController,
            GameDetailsDialogPresenter = selectionServices.GameDetailsDialogPresenter,
            SelectionPopupCoordinator = selectionServices.SelectionPopupCoordinator,
            GameCardSelectionStateController = selectionServices.GameCardSelectionStateController,
            MainSelectionInteractionController = selectionServices.MainSelectionInteractionController,
            MainSelectionRecomputeController = selectionServices.MainSelectionRecomputeController,
            MainLanguageChangeController = selectionServices.MainLanguageChangeController,
            MainVisibleGameCardRefreshController = selectionServices.MainVisibleGameCardRefreshController
        };
    }
}

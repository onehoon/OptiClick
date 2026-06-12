using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private RelayCommand _openGameSupportRequestCommand = null!;

    private void InitializeCommandSet()
    {
        Commands = new ShellCommandsViewModel(
            RequestSetCurrentViewAsync,
            ShowScanViewAsync,
            ex => LogError(MainViewModelLogCategories.Command, "navigation command failed", ex));
        _openGameSupportRequestCommand = new RelayCommand(_ => OpenGameSupportRequest());
    }

    private async Task RequestSetCurrentViewAsync(
        ShellViewKind view,
        CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOptiScalerChangesIfNeededAsync(view, cancellationToken))
        {
            return;
        }

        SetCurrentView(view);
    }

    private async Task ShowScanViewAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOptiScalerChangesIfNeededAsync(ShellViewKind.Scan, cancellationToken))
        {
            return;
        }

        ScanStatusText = Strings.ScanChooseAndSave;
        SetCurrentView(ShellViewKind.Scan);
    }

    private async Task<bool> ConfirmDiscardOptiScalerChangesIfNeededAsync(
        ShellViewKind targetView,
        CancellationToken cancellationToken)
    {
        return await _features.ShellInteraction.ConfirmOptiScalerDirtyNavigationAsync(
            Navigation.CurrentViewKind,
            targetView,
            OptiScaler.HasUnsavedChanges,
            new OptiScalerDirtyNavigationGuardText
            {
                Title = Strings.OptiScalerDiscardChangesTitle,
                Summary = Strings.OptiScalerDiscardChangesSummary,
                PrimaryButtonText = Strings.OptiScalerDiscardChangesPrimaryButton,
                SecondaryButtonText = Strings.OptiScalerDiscardChangesSecondaryButton
            },
            OptiScaler.SaveChanges,
            OptiScaler.DiscardChanges,
            cancellationToken);
    }

    private void RefreshNavigationAndScanCommandStates()
    {
        Commands?.RefreshNavigationCommandStates();
        Scan?.RefreshCommandStates();
        Home?.RefreshCommandStates();
    }
}

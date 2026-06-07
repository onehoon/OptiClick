using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Models;
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
        if (Navigation.CurrentViewKind != ShellViewKind.OptiScaler
            || targetView == ShellViewKind.OptiScaler
            || !OptiScaler.HasUnsavedChanges)
        {
            return true;
        }

        var result = await _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = Strings.OptiScalerDiscardChangesTitle,
                Summary = Strings.OptiScalerDiscardChangesSummary,
                PrimaryButtonText = Strings.OptiScalerDiscardChangesPrimaryButton,
                SecondaryButtonText = Strings.OptiScalerDiscardChangesSecondaryButton,
                PrimaryResult = AppDialogResult.Ok,
                SecondaryResult = AppDialogResult.Continue,
                CanClose = true
            },
            cancellationToken);

        if (result == AppDialogResult.Ok)
        {
            OptiScaler.SaveChanges();
            return true;
        }

        if (result == AppDialogResult.Continue)
        {
            OptiScaler.DiscardChanges();
            return true;
        }

        return false;
    }

    private void RefreshNavigationAndScanCommandStates()
    {
        Commands?.RefreshNavigationCommandStates();
        Scan?.RefreshCommandStates();
        Home?.RefreshCommandStates();
    }
}

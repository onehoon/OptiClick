using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void ShowScanView() { ScanStatusText = Strings.ScanChooseAndSave; SetCurrentView(ShellViewKind.Scan); }

    private GameCardViewModel? ReplaceGameCards(
        IReadOnlyList<GameCardViewModel> cards,
        bool observeAutoSelection = true)
    {
        return _features.Selection.ReplaceGameCards(cards, observeAutoSelection);
    }

    private async Task ObserveAutoSelectionAsync(GameCardViewModel selected, bool navigateHome)
    {
        await _features.Selection.ObserveAutoSelectionAsync(selected, navigateHome, CancellationToken.None);
    }

    private void SetSelectedGame(GameCardViewModel? selectedGame)
    {
        _features.Selection.ApplySelectionState(Games, selectedGame);
        SelectedGame = selectedGame;
    }

    public void ConfirmNextPendingPopup()
    {
        _features.Selection.ConfirmNextPendingPopup();
    }

    private async Task SelectGameCardAsync(
        GameCardViewModel? game,
        CancellationToken cancellationToken = default,
        bool navigateHome = true,
        bool showPendingPopups = true)
    {
        await _features.Selection.SelectGameCardAsync(
            game,
            cancellationToken,
            navigateHome,
            showPendingPopups);
    }
    // Match result is intentionally not synthesized from card selection alone.
    // It must come from an actual scan/match pipeline.

    private void RelocalizeScanFolderRows()
    {
        Scan.RelocalizeScanFolderRows();
    }

    private void ApplyScanFolderStateUpdate(ScanFolderStateUpdate update)
    {
        Scan.ApplyScanFolderStateUpdate(update);
    }

    public async Task SaveAndStartScanAsync(CancellationToken cancellationToken = default)
    {
        await Scan.SaveAndStartScanAsync(cancellationToken);
    }

    private async Task RunStartupAutoScanAsync(CancellationToken cancellationToken = default)
    {
        await Scan.RunStartupAutoScanAsync(cancellationToken);
    }

    private async Task RecomputeSelectionAfterScanAsync(CancellationToken cancellationToken, bool navigateHome)
    {
        await _features.Selection.RecomputeSelectionAfterScanAsync(cancellationToken, navigateHome);
    }
}

using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanResultCoordinator
{
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly Func<ScanFlowResult, MainViewModelStateUpdate> _createScanStateUpdate;
    private readonly DialogPresenter _dialogPresenter;
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Func<int> _gameCountAccessor;
    private readonly Func<string> _remoteCatalogErrorCodeAccessor;
    private readonly Func<bool> _readSuppressHomeNavigationForAutoSelection;
    private readonly Action<bool> _setSuppressHomeNavigationForAutoSelection;
    private readonly Action<MainViewModelStateUpdate> _applyStateUpdate;
    private readonly Action<ShellViewKind> _setCurrentView;
    private readonly Func<CancellationToken, bool, Task> _recomputeSelectionAfterScanAsync;
    private readonly string _flowLogFallbackCategory;

    public ScanResultCoordinator(ScanResultCoordinatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _flowLogDispatcher = options.FlowLogDispatcher ?? throw new ArgumentNullException(nameof(options.FlowLogDispatcher));
        _createScanStateUpdate = options.CreateScanStateUpdate ?? throw new ArgumentNullException(nameof(options.CreateScanStateUpdate));
        _dialogPresenter = options.DialogPresenter ?? throw new ArgumentNullException(nameof(options.DialogPresenter));
        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _gameCountAccessor = options.GameCountAccessor ?? throw new ArgumentNullException(nameof(options.GameCountAccessor));
        _remoteCatalogErrorCodeAccessor = options.RemoteCatalogErrorCodeAccessor ?? throw new ArgumentNullException(nameof(options.RemoteCatalogErrorCodeAccessor));
        _readSuppressHomeNavigationForAutoSelection = options.ReadSuppressHomeNavigationForAutoSelection ?? throw new ArgumentNullException(nameof(options.ReadSuppressHomeNavigationForAutoSelection));
        _setSuppressHomeNavigationForAutoSelection = options.SetSuppressHomeNavigationForAutoSelection ?? throw new ArgumentNullException(nameof(options.SetSuppressHomeNavigationForAutoSelection));
        _applyStateUpdate = options.ApplyStateUpdate ?? throw new ArgumentNullException(nameof(options.ApplyStateUpdate));
        _setCurrentView = options.SetCurrentView ?? throw new ArgumentNullException(nameof(options.SetCurrentView));
        _recomputeSelectionAfterScanAsync = options.RecomputeSelectionAfterScanAsync ?? throw new ArgumentNullException(nameof(options.RecomputeSelectionAfterScanAsync));
        _flowLogFallbackCategory = string.IsNullOrWhiteSpace(options.FlowLogFallbackCategory)
            ? MainViewModelLogCategories.Scan
            : options.FlowLogFallbackCategory;
    }

    public Task ApplyManualScanResultAsync(
        ScanFlowResult result,
        CancellationToken cancellationToken)
    {
        return ApplyScanFlowResultAsync(result, cancellationToken, navigateHome: true);
    }

    public async Task ApplyStartupAutoScanResultAsync(
        ScanFlowResult result,
        CancellationToken cancellationToken)
    {
        await ApplyScanFlowResultAsync(result, cancellationToken, navigateHome: false);
        ApplyStartupNoGamesNavigation(result);
        await ShowStartupNoSupportedGamesGuidanceAsync(result, cancellationToken);
    }

    public async Task RunWithStartupAutoSelectionSuppressedAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var previousSuppressFlag = _readSuppressHomeNavigationForAutoSelection();
        _setSuppressHomeNavigationForAutoSelection(true);
        try
        {
            await operation(cancellationToken);
        }
        finally
        {
            _setSuppressHomeNavigationForAutoSelection(previousSuppressFlag);
        }
    }

    private async Task ApplyScanFlowResultAsync(
        ScanFlowResult result,
        CancellationToken cancellationToken,
        bool navigateHome)
    {
        _flowLogDispatcher.Dispatch(result.Logs, _flowLogFallbackCategory);
        var update = _createScanStateUpdate(result);
        _applyStateUpdate(update);

        if (update.DialogRequest is not null)
        {
            await _dialogPresenter.ShowSafelyAsync(update.DialogRequest, cancellationToken);
        }

        if (update.ShouldRecomputeSelection)
        {
            await _recomputeSelectionAfterScanAsync(cancellationToken, navigateHome);
        }

        if (update.ShouldNavigateHome && _gameCountAccessor() > 0)
        {
            _setCurrentView(ShellViewKind.Home);
        }
    }

    private void ApplyStartupNoGamesNavigation(ScanFlowResult result)
    {
        if (!ShouldNavigateToScanForStartupNoGames(result))
        {
            return;
        }

        _setCurrentView(ShellViewKind.Scan);
    }

    private async Task ShowStartupNoSupportedGamesGuidanceAsync(
        ScanFlowResult result,
        CancellationToken cancellationToken)
    {
        if (!ShouldShowStartupNoSupportedGamesGuidance(result))
        {
            return;
        }

        _setCurrentView(ShellViewKind.Scan);
        var strings = _stringsAccessor();
        await _dialogPresenter.ShowSafelyAsync(
            new AppDialogRequest
            {
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                Title = strings.NavScan,
                Summary = strings.ScanNoSupportedGamesFoundGuide
            },
            cancellationToken);
    }

    private bool ShouldNavigateToScanForStartupNoGames(ScanFlowResult result)
    {
        if (result.Summary.MatchedCount > 0 || _gameCountAccessor() > 0)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(_remoteCatalogErrorCodeAccessor());
    }

    private bool ShouldShowStartupNoSupportedGamesGuidance(ScanFlowResult result)
    {
        if (!ShouldNavigateToScanForStartupNoGames(result))
        {
            return false;
        }

        return result.DidRun;
    }
}

public sealed record ScanResultCoordinatorOptions
{
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required string FlowLogFallbackCategory { get; init; }
    public required Func<ScanFlowResult, MainViewModelStateUpdate> CreateScanStateUpdate { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required Func<int> GameCountAccessor { get; init; }
    public required Func<string> RemoteCatalogErrorCodeAccessor { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<bool> SetSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
}

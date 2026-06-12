using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainScanShellFacade
{
    private readonly MainScanResolvedDependencies _scanDependencies;
    private readonly DialogPresenter _dialogPresenter;
    private readonly FlowLogDispatcher _flowLogDispatcher;
    private readonly MainViewModelResultApplier _resultApplier;

    private MainScanShellFacade(MainScanShellFacadeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _scanDependencies = input.ScanDependencies ?? throw new ArgumentNullException(nameof(input.ScanDependencies));
        _dialogPresenter = input.DialogPresenter ?? throw new ArgumentNullException(nameof(input.DialogPresenter));
        _flowLogDispatcher = input.FlowLogDispatcher ?? throw new ArgumentNullException(nameof(input.FlowLogDispatcher));
        _resultApplier = input.ResultApplier ?? throw new ArgumentNullException(nameof(input.ResultApplier));
    }

    public static MainScanShellFacade Create(MainScanShellFacadeInput input)
    {
        return new MainScanShellFacade(input);
    }

    public ScanSectionCompositionInput CreateSectionCompositionInput(MainScanSectionCompositionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ScanSectionCompositionInput
        {
            ScanFolderDiscoveryService = _scanDependencies.ScanFolderDiscoveryService,
            ScanResultCoordinatorFactory = _scanDependencies.ScanResultCoordinatorFactory,
            ScanOrchestratorFactory = _scanDependencies.ScanOrchestratorFactory,
            ScanFlowController = _scanDependencies.ScanFlowController,
            ScanFolderListController = _scanDependencies.ScanFolderListController,
            ScanFolderActionController = _scanDependencies.ScanFolderActionController,
            ScanLock = context.ScanLock,
            ScannedGameState = context.ScannedGameState,
            DialogPresenter = _dialogPresenter,
            FlowLogDispatcher = _flowLogDispatcher,
            CreateScanStateUpdate = _resultApplier.CreateScanStateUpdate,
            RemoteCatalogErrorCodeAccessor = context.ReadRemoteCatalogErrorCode,
            ReadSuppressHomeNavigationForAutoSelection = context.ReadSuppressHomeNavigationForAutoSelection,
            SetSuppressHomeNavigationForAutoSelection = context.SetSuppressHomeNavigationForAutoSelection,
            ApplyStateUpdate = context.ApplyStateUpdate,
            SetCurrentView = context.SetCurrentView,
            RecomputeSelectionAfterScanAsync = context.RecomputeSelectionAfterScanAsync,
            IsMultiGpuBlocked = context.IsMultiGpuBlocked,
            BuildScanRequest = context.BuildScanRequest,
            ClearVisibleGameCards = context.ClearVisibleGameCards,
            LogScanWarning = context.LogScanWarning,
            ApplyInitialScanFolderLoadResult = ApplyInitialScanFolderLoadResult,
            ApplyScanFolderActionResult = result => context.ApplyDeferredStateUpdate(
                _resultApplier.CreateScanFolderActionStateUpdate(result)),
            ShowHome = () => context.SetCurrentView(ShellViewKind.Home),
            OnScanCommandException = context.LogScanCommandException,
            ScanLogCategory = MainViewModelLogCategories.Scan
        };
    }

    private IReadOnlyList<ScanFolderRowViewModel> ApplyInitialScanFolderLoadResult(ScanFolderActionResult result)
    {
        var update = _resultApplier.CreateScanFolderActionStateUpdate(result);
        _flowLogDispatcher.Dispatch(update.FlowLogs, MainViewModelLogCategories.Scan);
        return update.ScanFolderStateUpdate?.AddedFolders ?? [];
    }
}

internal sealed record MainScanShellFacadeInput
{
    public required MainScanResolvedDependencies ScanDependencies { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
}

internal sealed record MainScanSectionCompositionContext
{
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required Func<string> ReadRemoteCatalogErrorCode { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<bool> SetSuppressHomeNavigationForAutoSelection { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required Action ClearVisibleGameCards { get; init; }
    public required Action<string> LogScanWarning { get; init; }
    public required Action<Exception> LogScanCommandException { get; init; }
}

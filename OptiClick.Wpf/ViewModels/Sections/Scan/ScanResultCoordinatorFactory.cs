using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Navigation;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanResultCoordinatorFactory
{
    public ScanResultCoordinator Create(ScanResultCoordinatorFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ScanResultCoordinator(
            new ScanResultCoordinatorOptions
            {
                FlowLogDispatcher = input.FlowLogDispatcher,
                FlowLogFallbackCategory = input.FlowLogFallbackCategory,
                ResultApplier = input.ResultApplier,
                DialogPresenter = input.DialogPresenter,
                StringsAccessor = input.StringsAccessor,
                GameCountAccessor = input.GameCountAccessor,
                RemoteCatalogErrorCodeAccessor = input.RemoteCatalogErrorCodeAccessor,
                ReadSuppressHomeNavigationForAutoSelection = input.ReadSuppressHomeNavigationForAutoSelection,
                SetSuppressHomeNavigationForAutoSelection = input.SetSuppressHomeNavigationForAutoSelection,
                ApplyStateUpdate = input.ApplyStateUpdate,
                SetCurrentView = input.SetCurrentView,
                RecomputeSelectionAfterScanAsync = input.RecomputeSelectionAfterScanAsync
            });
    }
}

public sealed record ScanResultCoordinatorFactoryInput
{
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required string FlowLogFallbackCategory { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
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

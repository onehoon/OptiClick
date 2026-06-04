using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanOrchestratorFactory
{
    public ScanOrchestrator Create(ScanOrchestratorFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ScanOrchestrator(
            new ScanOrchestratorOptions
            {
                StringsAccessor = input.StringsAccessor,
                ScanFlowController = input.ScanFlowController,
                ScanLock = input.ScanLock,
                ScannedGameState = input.ScannedGameState,
                DialogPresenter = input.DialogPresenter,
                IsMultiGpuBlocked = input.IsMultiGpuBlocked,
                BuildScanRequest = input.BuildScanRequest,
                ScanResultCoordinator = input.ScanResultCoordinator,
                ClearVisibleGameCards = input.ClearVisibleGameCards,
                LogWarning = input.LogWarning
            });
    }
}

public sealed record ScanOrchestratorFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required ScanResultCoordinator ScanResultCoordinator { get; init; }
    public required Action ClearVisibleGameCards { get; init; }
    public required Action<string> LogWarning { get; init; }
}

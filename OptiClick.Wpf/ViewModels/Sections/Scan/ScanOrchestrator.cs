using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanOrchestrator
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly ScanFlowController _scanFlowController;
    private readonly SemaphoreSlim _scanLock;
    private readonly ScannedGameState _scannedGameState;
    private readonly DialogPresenter _dialogPresenter;
    private readonly Func<bool> _isMultiGpuBlocked;
    private readonly Func<IReadOnlyList<string>, ScanFlowRequest> _buildScanRequest;
    private readonly Func<ScanFlowResult, CancellationToken, bool, Task> _applyScanFlowResultAsync;
    private readonly Func<Func<CancellationToken, Task>, CancellationToken, Task> _runWithStartupAutoSelectionSuppressedAsync;
    private readonly Action<ScanFlowResult> _applyStartupNoGamesNavigation;
    private readonly Func<ScanFlowResult, CancellationToken, Task> _showStartupNoSupportedGamesGuidanceAsync;
    private readonly Action _clearVisibleGameCards;
    private readonly Action<string> _logWarning;

    internal ScanOrchestrator(ScanOrchestratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _scanFlowController = options.ScanFlowController ?? throw new ArgumentNullException(nameof(options.ScanFlowController));
        _scanLock = options.ScanLock ?? throw new ArgumentNullException(nameof(options.ScanLock));
        _scannedGameState = options.ScannedGameState ?? throw new ArgumentNullException(nameof(options.ScannedGameState));
        _dialogPresenter = options.DialogPresenter ?? throw new ArgumentNullException(nameof(options.DialogPresenter));
        _isMultiGpuBlocked = options.IsMultiGpuBlocked ?? throw new ArgumentNullException(nameof(options.IsMultiGpuBlocked));
        _buildScanRequest = options.BuildScanRequest ?? throw new ArgumentNullException(nameof(options.BuildScanRequest));
        _applyScanFlowResultAsync = options.ApplyScanFlowResultAsync ?? throw new ArgumentNullException(nameof(options.ApplyScanFlowResultAsync));
        _runWithStartupAutoSelectionSuppressedAsync = options.RunWithStartupAutoSelectionSuppressedAsync ?? throw new ArgumentNullException(nameof(options.RunWithStartupAutoSelectionSuppressedAsync));
        _applyStartupNoGamesNavigation = options.ApplyStartupNoGamesNavigation ?? throw new ArgumentNullException(nameof(options.ApplyStartupNoGamesNavigation));
        _showStartupNoSupportedGamesGuidanceAsync = options.ShowStartupNoSupportedGamesGuidanceAsync ?? throw new ArgumentNullException(nameof(options.ShowStartupNoSupportedGamesGuidanceAsync));
        _clearVisibleGameCards = options.ClearVisibleGameCards ?? throw new ArgumentNullException(nameof(options.ClearVisibleGameCards));
        _logWarning = options.LogWarning ?? throw new ArgumentNullException(nameof(options.LogWarning));
    }

    public async Task SaveAndStartScanAsync(
        ScanOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var strings = _stringsAccessor();
        if (_isMultiGpuBlocked())
        {
            context.SetScanStatusText(strings.ScanBlockedUnsupportedGpuConfiguration);
            await _dialogPresenter.ShowSafelyAsync(
                new AppDialogRequest
                {
                    Kind = AppDialogKind.Warning,
                    Severity = DialogSeverity.Warning,
                    Title = strings.GpuUnsupportedConfigurationTitle,
                    Summary = strings.ScanBlockedUnsupportedGpuConfiguration
                },
                cancellationToken);
            return;
        }

        context.SaveScanFoldersToManifest();
        if (!context.HasAnyEnabledScanFolders())
        {
            _scannedGameState.Clear();
            _clearVisibleGameCards();
            context.SetScanStatusText(strings.ScanNoFolderSelected);
            await _dialogPresenter.ShowSafelyAsync(
                new AppDialogRequest
                {
                    Kind = AppDialogKind.Warning,
                    Severity = DialogSeverity.Warning,
                    Title = strings.NavScan,
                    Summary = strings.ScanNoFolderSelected
                },
                cancellationToken);
            return;
        }

        await _scanLock.TryRunExclusiveAsync(
            async ct =>
            {
                context.SetScanStatusText(_stringsAccessor().ScanInProgress);
                var result = await _scanFlowController.RunManualScanAsync(
                    _buildScanRequest(context.ResolveScanFolders()),
                    ct);
                await _applyScanFlowResultAsync(result, ct, true);
            },
            cancellationToken);
    }

    public async Task RunStartupAutoScanAsync(
        ScanOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var strings = _stringsAccessor();
        if (_isMultiGpuBlocked())
        {
            context.SetScanStatusText(strings.ScanBlockedUnsupportedGpuConfiguration);
            _logWarning("startup auto scan skipped reason=multi_gpu_blocked");
            return;
        }

        var ran = await _scanLock.TryRunExclusiveAsync(
            async ct =>
            {
                await _runWithStartupAutoSelectionSuppressedAsync(
                    async innerCt =>
                    {
                        var result = await _scanFlowController.RunStartupAutoScanAsync(
                            _buildScanRequest(context.ResolveScanFolders()),
                            innerCt);
                        await _applyScanFlowResultAsync(result, innerCt, false);
                        _applyStartupNoGamesNavigation(result);
                        await _showStartupNoSupportedGamesGuidanceAsync(result, innerCt);
                    },
                    ct);
            },
            cancellationToken);
        if (!ran)
        {
            _logWarning("startup auto scan skipped reason=scan_lock_busy");
        }
    }
}

internal sealed record ScanOrchestratorOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required SemaphoreSlim ScanLock { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required Func<bool> IsMultiGpuBlocked { get; init; }
    public required Func<IReadOnlyList<string>, ScanFlowRequest> BuildScanRequest { get; init; }
    public required Func<ScanFlowResult, CancellationToken, bool, Task> ApplyScanFlowResultAsync { get; init; }
    public required Func<Func<CancellationToken, Task>, CancellationToken, Task> RunWithStartupAutoSelectionSuppressedAsync { get; init; }
    public required Action<ScanFlowResult> ApplyStartupNoGamesNavigation { get; init; }
    public required Func<ScanFlowResult, CancellationToken, Task> ShowStartupNoSupportedGamesGuidanceAsync { get; init; }
    public required Action ClearVisibleGameCards { get; init; }
    public required Action<string> LogWarning { get; init; }
}

public sealed record ScanOrchestratorContext
{
    public required Action SaveScanFoldersToManifest { get; init; }
    public required Func<bool> HasAnyEnabledScanFolders { get; init; }
    public required Func<IReadOnlyList<string>> ResolveScanFolders { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
}

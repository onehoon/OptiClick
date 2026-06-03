using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly ScanFolderListController _scanFolderListController;
    private readonly ScanFolderActionController _scanFolderActionController;
    private readonly Action<ScanFolderActionResult> _applyScanFolderActionResult;
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
    private readonly Action _showHome;
    private readonly Brush _addedFolderStatusBrush;
    private readonly Brush _missingFolderStatusBrush;
    private string _scanStatusText = "";

    public ScanSectionViewModel(ScanSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _scanFolderListController = options.ScanFolderListController ?? throw new ArgumentNullException(nameof(options.ScanFolderListController));
        _scanFolderActionController = options.ScanFolderActionController ?? throw new ArgumentNullException(nameof(options.ScanFolderActionController));
        _applyScanFolderActionResult = options.ApplyScanFolderActionResult ?? throw new ArgumentNullException(nameof(options.ApplyScanFolderActionResult));
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
        _showHome = options.ShowHome ?? throw new ArgumentNullException(nameof(options.ShowHome));
        _addedFolderStatusBrush = options.AddedFolderStatusBrush ?? throw new ArgumentNullException(nameof(options.AddedFolderStatusBrush));
        _missingFolderStatusBrush = options.MissingFolderStatusBrush ?? throw new ArgumentNullException(nameof(options.MissingFolderStatusBrush));

        DefaultFolders = options.DefaultFolders ?? throw new ArgumentNullException(nameof(options.DefaultFolders));
        AddedFolders = options.AddedFolders ?? throw new ArgumentNullException(nameof(options.AddedFolders));

        AddScanFolderCommand = new RelayCommand(_ => AddScanFolder());
        RemoveScanFolderCommand = new RelayCommand(RemoveScanFolder);
        OpenScanFolderCommand = new RelayCommand(OpenScanFolder);
        SaveAndScanCommand = new AsyncRelayCommand(
            (_, cancellationToken) => SaveAndStartScanAsync(cancellationToken),
            _ => HasAnyEnabledScanFolders(),
            onException: options.OnCommandException);
        ShowHomeCommand = new RelayCommand(_ => _showHome());

        DefaultFolders.CollectionChanged += OnScanFoldersCollectionChanged;
        AddedFolders.CollectionChanged += OnScanFoldersCollectionChanged;
        InitializeScanFolderStateBindings();
    }

    public AppStrings Strings => _stringsAccessor();

    public ObservableCollection<ScanFolderRowViewModel> DefaultFolders { get; }

    public ObservableCollection<ScanFolderRowViewModel> AddedFolders { get; }

    public string ScanStatusText
    {
        get => _scanStatusText;
        set => SetProperty(ref _scanStatusText, value);
    }

    public Visibility DefaultFoldersEmptyVisibility => DefaultFolders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AddedFoldersEmptyVisibility => AddedFolders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public RelayCommand AddScanFolderCommand { get; }

    public RelayCommand RemoveScanFolderCommand { get; }

    public RelayCommand OpenScanFolderCommand { get; }

    public AsyncRelayCommand SaveAndScanCommand { get; }

    public RelayCommand ShowHomeCommand { get; }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Strings));
    }

    public string[] ResolveScanFolders()
    {
        return _scanFolderListController.ResolveScanFolders(DefaultFolders, AddedFolders);
    }

    public bool HasAnyEnabledScanFolders()
    {
        return DefaultFolders.Any(static folder => folder.IsChecked)
               || AddedFolders.Any(static folder => folder.IsChecked);
    }

    public void SaveScanFoldersToManifest()
    {
        _scanFolderListController.SaveFoldersToManifest(DefaultFolders, AddedFolders);
    }

    public async Task SaveAndStartScanAsync(CancellationToken cancellationToken = default)
    {
        if (_isMultiGpuBlocked())
        {
            ScanStatusText = Strings.ScanBlockedUnsupportedGpuConfiguration;
            await _dialogPresenter.ShowSafelyAsync(
                new AppDialogRequest
                {
                    Kind = AppDialogKind.Warning,
                    Severity = DialogSeverity.Warning,
                    Title = Strings.GpuUnsupportedConfigurationTitle,
                    Summary = Strings.ScanBlockedUnsupportedGpuConfiguration
                },
                cancellationToken);
            return;
        }

        SaveScanFoldersToManifest();
        if (!HasAnyEnabledScanFolders())
        {
            _scannedGameState.Clear();
            _clearVisibleGameCards();
            ScanStatusText = Strings.ScanNoFolderSelected;
            await _dialogPresenter.ShowSafelyAsync(
                new AppDialogRequest
                {
                    Kind = AppDialogKind.Warning,
                    Severity = DialogSeverity.Warning,
                    Title = Strings.NavScan,
                    Summary = Strings.ScanNoFolderSelected
                },
                cancellationToken);
            return;
        }

        await _scanLock.TryRunExclusiveAsync(
            async ct =>
            {
                ScanStatusText = Strings.ScanInProgress;
                var result = await _scanFlowController.RunManualScanAsync(
                    _buildScanRequest(ResolveScanFolders()),
                    ct);
                await _applyScanFlowResultAsync(result, ct, true);
            },
            cancellationToken);
    }

    public async Task RunStartupAutoScanAsync(CancellationToken cancellationToken = default)
    {
        if (_isMultiGpuBlocked())
        {
            ScanStatusText = Strings.ScanBlockedUnsupportedGpuConfiguration;
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
                            _buildScanRequest(ResolveScanFolders()),
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

    public void ApplyScanFolderStateUpdate(ScanFolderStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (update.DefaultFolders is not null)
        {
            ReplaceScanFolderRows(DefaultFolders, update.DefaultFolders);
        }

        if (update.AddedFolders is not null)
        {
            ReplaceScanFolderRows(AddedFolders, update.AddedFolders);
        }
    }

    public void RelocalizeScanFolderRows()
    {
        ApplyScanFolderActionResult(_scanFolderActionController.RelocalizeRows(
            DefaultFolders,
            AddedFolders,
            Strings,
            _addedFolderStatusBrush,
            _missingFolderStatusBrush));
    }

    public void RefreshCommandStates()
    {
        SaveAndScanCommand.RaiseCanExecuteChanged();
    }

    private void AddScanFolder()
    {
        ApplyScanFolderActionResult(_scanFolderActionController.AddFolder(
            DefaultFolders,
            AddedFolders,
            Strings,
            _addedFolderStatusBrush));
    }

    private void RemoveScanFolder(object? parameter)
    {
        ApplyScanFolderActionResult(_scanFolderActionController.RemoveFolder(
            parameter as ScanFolderRowViewModel,
            DefaultFolders,
            AddedFolders,
            Strings));
    }

    private void OpenScanFolder(object? parameter)
    {
        ApplyScanFolderActionResult(_scanFolderActionController.OpenFolder(
            parameter as ScanFolderRowViewModel,
            Strings));
    }

    private void ApplyScanFolderActionResult(ScanFolderActionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _applyScanFolderActionResult(result);
    }

    private void OnScanFoldersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<ScanFolderRowViewModel>())
            {
                oldItem.PropertyChanged -= OnScanFolderRowPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<ScanFolderRowViewModel>())
            {
                newItem.PropertyChanged += OnScanFolderRowPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(DefaultFoldersEmptyVisibility));
        OnPropertyChanged(nameof(AddedFoldersEmptyVisibility));
        RefreshCommandStates();
    }

    private void OnScanFolderRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(ScanFolderRowViewModel.IsChecked), StringComparison.Ordinal))
        {
            return;
        }

        RefreshCommandStates();
        SaveScanFoldersToManifest();
    }

    private void InitializeScanFolderStateBindings()
    {
        foreach (var row in DefaultFolders)
        {
            row.PropertyChanged += OnScanFolderRowPropertyChanged;
        }

        foreach (var row in AddedFolders)
        {
            row.PropertyChanged += OnScanFolderRowPropertyChanged;
        }

        RefreshCommandStates();
    }

    private static void ReplaceScanFolderRows(
        ObservableCollection<ScanFolderRowViewModel> target,
        IReadOnlyList<ScanFolderRowViewModel> rows)
    {
        target.Clear();
        foreach (var row in rows)
        {
            target.Add(row);
        }
    }
}

public sealed record ScanSectionViewModelOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> DefaultFolders { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> AddedFolders { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required Action<ScanFolderActionResult> ApplyScanFolderActionResult { get; init; }
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
    public required Action ShowHome { get; init; }
    public required Brush AddedFolderStatusBrush { get; init; }
    public required Brush MissingFolderStatusBrush { get; init; }
    public Action<Exception>? OnCommandException { get; init; }
}

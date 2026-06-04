using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels.Sections.Scan;

public sealed class ScanSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly ScanFolderListController _scanFolderListController;
    private readonly ScanFolderActionController _scanFolderActionController;
    private readonly Action<ScanFolderActionResult> _applyScanFolderActionResult;
    private readonly ScanOrchestrator _scanOrchestrator;
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
        _scanOrchestrator = options.ScanOrchestrator ?? throw new ArgumentNullException(nameof(options.ScanOrchestrator));
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

    public Task SaveAndStartScanAsync(CancellationToken cancellationToken = default)
    {
        return _scanOrchestrator.SaveAndStartScanAsync(CreateScanOrchestratorContext(), cancellationToken);
    }

    public Task RunStartupAutoScanAsync(CancellationToken cancellationToken = default)
    {
        return _scanOrchestrator.RunStartupAutoScanAsync(CreateScanOrchestratorContext(), cancellationToken);
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

    private ScanOrchestratorContext CreateScanOrchestratorContext()
    {
        return new ScanOrchestratorContext
        {
            SaveScanFoldersToManifest = SaveScanFoldersToManifest,
            HasAnyEnabledScanFolders = HasAnyEnabledScanFolders,
            ResolveScanFolders = () => ResolveScanFolders(),
            SetScanStatusText = value => ScanStatusText = value
        };
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
    public required ScanOrchestrator ScanOrchestrator { get; init; }
    public required Action ShowHome { get; init; }
    public required Brush AddedFolderStatusBrush { get; init; }
    public required Brush MissingFolderStatusBrush { get; init; }
    public Action<Exception>? OnCommandException { get; init; }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Threading;

namespace OptiClick.Wpf.ViewModels.Sections.Home;

public sealed class HomeSectionViewModel : ViewModelBase
{
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Func<bool> _canSelectGame;
    private readonly Func<bool> _canShowDetails;
    private readonly Func<bool> _canShowInstall;
    private readonly Action _showDetails;
    private readonly Func<CancellationToken, Task> _showInstallAsync;
    private readonly Func<GameCardViewModel, CancellationToken, Task> _selectGameAsync;
    private GameCardViewModel? _selectedGame;

    public HomeSectionViewModel(HomeSectionViewModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _stringsAccessor = options.StringsAccessor ?? throw new ArgumentNullException(nameof(options.StringsAccessor));
        _canSelectGame = options.CanSelectGame ?? (() => true);
        _canShowDetails = options.CanShowDetails ?? (() => SelectedGame is not null);
        _canShowInstall = options.CanShowInstall ?? (() => SelectedGame is not null);
        _showDetails = options.ShowDetails ?? throw new ArgumentNullException(nameof(options.ShowDetails));
        _showInstallAsync = options.ShowInstallAsync ?? throw new ArgumentNullException(nameof(options.ShowInstallAsync));
        _selectGameAsync = options.SelectGameAsync ?? throw new ArgumentNullException(nameof(options.SelectGameAsync));
        Games = options.Games ?? throw new ArgumentNullException(nameof(options.Games));
        SelectedGameAction = options.SelectedGameAction ?? new SelectedGameActionViewModel();

        SelectGameCommand = new AsyncRelayCommand(
            async (parameter, cancellationToken) =>
            {
                if (parameter is GameCardViewModel game)
                {
                    await _selectGameAsync(game, cancellationToken);
                }
            },
            _ => _canSelectGame(),
            onException: options.OnSelectGameException,
            allowConcurrentExecutions: true);
        ShowDetailsCommand = new RelayCommand(
            _ => _showDetails(),
            _ => _canShowDetails());
        ShowInstallCommand = new AsyncRelayCommand(
            (_, cancellationToken) => _showInstallAsync(cancellationToken),
            _ => _canShowInstall(),
            onException: options.OnShowInstallException);

        Games.CollectionChanged += Games_OnCollectionChanged;
    }

    public event EventHandler? VisibleCoverLoadRequested;

    public AppStrings Strings => _stringsAccessor();

    public ObservableCollection<GameCardViewModel> Games { get; }

    public SelectedGameActionViewModel SelectedGameAction { get; }

    public GameCardViewModel? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (!SetProperty(ref _selectedGame, value))
            {
                return;
            }

            SelectedGameAction.Update(value);
            OnPropertyChanged(nameof(HasGamesForHomeSelectionMessage));
            RefreshCommandStates();
        }
    }

    public bool HasGamesForHomeSelectionMessage => Games.Count > 0;

    public ICommand SelectGameCommand { get; }

    public ICommand ShowDetailsCommand { get; }

    public ICommand ShowInstallCommand { get; }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Strings));
        SelectedGameAction.ApplyLocalization(Strings);
    }

    public void RefreshCommandStates()
    {
        if (SelectGameCommand is AsyncRelayCommand selectGameCommand)
        {
            selectGameCommand.RaiseCanExecuteChanged();
        }

        if (ShowDetailsCommand is RelayCommand showDetailsCommand)
        {
            showDetailsCommand.RaiseCanExecuteChanged();
        }

        if (ShowInstallCommand is AsyncRelayCommand showInstallCommand)
        {
            showInstallCommand.RaiseCanExecuteChanged();
        }
    }

    public void RequestVisibleCoverLoad()
    {
        VisibleCoverLoadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Games_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasGamesForHomeSelectionMessage));
    }
}

public sealed class HomeSectionViewModelOptions
{
    public required Func<AppStrings> StringsAccessor { get; init; }

    public required ObservableCollection<GameCardViewModel> Games { get; init; }

    public SelectedGameActionViewModel? SelectedGameAction { get; init; }

    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }

    public required Action ShowDetails { get; init; }

    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }

    public Func<bool>? CanSelectGame { get; init; }

    public Func<bool>? CanShowDetails { get; init; }

    public Func<bool>? CanShowInstall { get; init; }

    public Action<Exception>? OnSelectGameException { get; init; }

    public Action<Exception>? OnShowInstallException { get; init; }
}

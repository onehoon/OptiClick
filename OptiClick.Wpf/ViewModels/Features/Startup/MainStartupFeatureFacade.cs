using System.Collections.ObjectModel;
using OptiClick.Wpf.ViewModels;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Threading;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels.Features.Startup;
internal sealed class MainStartupFeatureFacade
{
    private readonly MainStartupFlowController _flowController;
    private readonly MainStartupDialogsController _dialogsController;
    private readonly MainStartupFlowContextFactory _flowContextFactory;
    private readonly StartupPreparationContextFactory _preparationContextFactory;
    private readonly GameMasterCoverPrefetchCoordinator _gameMasterCoverPrefetchCoordinator;
    private readonly StartupBackgroundTaskManager _backgroundTaskManager;
    private readonly StartupPreparationCoordinator _preparationCoordinator;
    private readonly MainStartupDialogsPort _dialogsPort;
    private readonly MainStartupCoverPrefetchPort _coverPrefetchPort;

    public MainStartupFeatureFacade(
        MainStartupFlowController flowController,
        MainStartupDialogsController dialogsController,
        MainStartupFlowContextFactory flowContextFactory,
        StartupPreparationContextFactory preparationContextFactory,
        GameMasterCoverPrefetchCoordinator gameMasterCoverPrefetchCoordinator,
        StartupBackgroundTaskManager backgroundTaskManager,
        StartupPreparationCoordinator preparationCoordinator,
        MainStartupDialogsPort dialogsPort,
        MainStartupCoverPrefetchPort coverPrefetchPort)
    {
        _flowController = flowController;
        _dialogsController = dialogsController;
        _flowContextFactory = flowContextFactory;
        _preparationContextFactory = preparationContextFactory;
        _gameMasterCoverPrefetchCoordinator = gameMasterCoverPrefetchCoordinator;
        _backgroundTaskManager = backgroundTaskManager;
        _preparationCoordinator = preparationCoordinator;
        _dialogsPort = dialogsPort;
        _coverPrefetchPort = coverPrefetchPort;
    }

    public Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(CancellationToken cancellationToken = default)
    {
        return _flowController.ShowStartupOperatingSystemBlockIfNeededAsync(
            _flowContextFactory.Create(),
            cancellationToken);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _flowController.InitializeAsync(
            _flowContextFactory.Create(),
            cancellationToken);
    }

    public Task StartStartupPreparationAsync(CancellationToken cancellationToken = default)
    {
        return _preparationCoordinator.StartStartupPreparationAsync(
            _preparationContextFactory.CreateRequest(),
            cancellationToken);
    }

    public void StartStartupDialogsInBackground()
    {
        _dialogsController.StartInBackground(new MainStartupDialogsContext
        {
            Services = new MainStartupDialogsServices
            {
                StartupBackgroundTaskManager = _backgroundTaskManager,
                ShowStartupAnnouncementIfNeededAsync = _dialogsPort.ShowStartupAnnouncementIfNeededAsync,
                ShowStartupUpdateCheckDialogAsync = _dialogsPort.ShowStartupUpdateCheckDialogAsync
            },
            Callbacks = new MainStartupDialogsCallbacks
            {
                UpdateStartupPreparationState = _dialogsPort.UpdateStartupPreparationState,
                ClearLastErrorCode = _dialogsPort.ClearLastErrorCode,
                LogWarning = _dialogsPort.LogWarning
            }
        });
    }

    public void StartGameMasterCoverPrefetchInBackground()
    {
        _gameMasterCoverPrefetchCoordinator.StartGameMasterCoverPrefetchInBackground(
            new GameMasterCoverPrefetchCoordinatorRequest
            {
                GameMasterAccessor = _coverPrefetchPort.GameMasterAccessor,
                HomeCardsAccessor = _coverPrefetchPort.HomeCardsAccessor,
                RefreshHomeCoversOnDispatcherAsync = _coverPrefetchPort.RefreshHomeCoversOnDispatcherAsync,
                UpdateStartupPreparationState = _coverPrefetchPort.UpdateStartupPreparationState,
                ClearLastErrorCode = _coverPrefetchPort.ClearLastErrorCode,
                LogInfo = _coverPrefetchPort.LogInfo,
                LogWarning = _coverPrefetchPort.LogWarning
            });
    }

    public void QueueHomeCoverPrefetchInBackground(string reason)
    {
        _gameMasterCoverPrefetchCoordinator.QueueHomeCoverPrefetchInBackground(
            new GameMasterHomeCoverPrefetchCoordinatorRequest
            {
                Reason = reason,
                GameMasterAccessor = _coverPrefetchPort.GameMasterAccessor,
                HomeCardsAccessor = _coverPrefetchPort.HomeCardsAccessor,
                RefreshHomeCoversOnDispatcherAsync = _coverPrefetchPort.RefreshHomeCoversOnDispatcherAsync,
                LogInfo = _coverPrefetchPort.LogInfo,
                LogWarning = _coverPrefetchPort.LogWarning
            });
    }

    public void CancelBackgroundWork()
    {
        _backgroundTaskManager.CancelAll();
    }

    public void StartBackgroundTask(Func<CancellationTokenSource, Task> runAsync)
    {
        var cancellationTokenSource = _backgroundTaskManager.CreateSource();
        _ = runAsync(cancellationTokenSource);
    }

    public void RemoveBackgroundTask(CancellationTokenSource cancellationTokenSource)
    {
        _backgroundTaskManager.Remove(cancellationTokenSource);
    }

}

internal sealed record MainStartupDialogsPort
{
    public required Func<CancellationToken, Task> ShowStartupAnnouncementIfNeededAsync { get; init; }
    public required Func<CancellationToken, Task> ShowStartupUpdateCheckDialogAsync { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<string> LogWarning { get; init; }
}

internal sealed record MainStartupCoverPrefetchPort
{
    public required Func<IReadOnlyList<RuntimeDataGameProfile>> GameMasterAccessor { get; init; }
    public required Func<IReadOnlyCollection<GameCardViewModel>> HomeCardsAccessor { get; init; }
    public required Func<Task> RefreshHomeCoversOnDispatcherAsync { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogWarning { get; init; }
}

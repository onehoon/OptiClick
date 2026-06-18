using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Threading;
using OptiClick.Wpf.ViewModels.Features.Install;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainInstallShellFacade
{
    public required MainInstallFeatureFacade Feature { get; init; }

    public static MainInstallShellFacade Create(MainInstallShellFacadeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.InstallDependencies);

        var shared = MainInstallContextFactorySharedComposition.Compose(
            new MainInstallContextFactorySharedCompositionInput
            {
                ApplyInstallBusyState = input.ApplyInstallBusyState,
                ReadInstallButtonText = input.ReadInstallButtonText,
                TryRefreshVisibleCard = input.TryRefreshVisibleCard,
                SelectGameAsync = input.SelectGameAsync,
                ReadModuleDownloadLinks = () => input.RuntimeShellState.ModuleDownloadLinks,
                DispatchFlowLogs = input.FlowLogDispatcher.Dispatch
            });

        MainInstallContextFactoryCompositionServices? contextFactories = null;
        contextFactories = MainInstallContextFactoryComposition.Compose(
            new MainInstallContextFactoryCompositionInput
            {
                InstallInteraction = CreateInstallInteractionInput(input),
                UninstallInteraction = CreateUninstallInteractionInput(input, shared),
                InstallCompletion = CreateInstallCompletionInput(input, shared),
                InstallArchiveReadiness = CreateInstallArchiveReadinessInput(input, shared),
                InstallPreparation = CreateInstallPreparationInput(input, shared),
                InstallExecutionBridge = CreateInstallExecutionBridgeInput(
                    input,
                    shared,
                    () => (contextFactories ?? throw new InvalidOperationException(
                        "Install context factories are not initialized."))
                        .InstallCompletion
                        .Create())
            });

        var dependencies = input.InstallDependencies;
        return new MainInstallShellFacade
        {
            Feature = new MainInstallFeatureFacade(
                dependencies.InstallPopupPresenter,
                dependencies.MainInstallArchiveReadinessController,
                dependencies.MainInstallPreparationController,
                dependencies.MainInstallExecutionBridge,
                dependencies.MainInstallInteractionController,
                dependencies.MainUninstallInteractionController,
                contextFactories.InstallInteraction,
                contextFactories.UninstallInteraction,
                contextFactories.InstallArchiveReadiness,
                contextFactories.InstallPreparation,
                contextFactories.InstallExecutionBridge)
        };
    }

    private static MainInstallInteractionCompositionInput CreateInstallInteractionInput(
        MainInstallShellFacadeInput input)
    {
        return new MainInstallInteractionCompositionInput
        {
            ShouldBlockStartupForUnsupportedOperatingSystem =
                input.ShouldBlockStartupForUnsupportedOperatingSystem,
            ResolveSelectedGame = input.ResolveSelectedGame,
            IsAppUpdateInProgress = input.IsAppUpdateInProgress,
            ReadSelectedInstallStatusCode = () => input.ReadSelectionState().SelectedInstallStatusCode,
            ReadSelectedGameId = () => input.ReadSelectionState().SelectedGameId ?? "",
            ShowStartupBlockDialogAsync = ct =>
                input.DialogPresenter.ShowSafelyAsync(
                    input.StartupNoticePresenter.BuildWindows10StartupBlockDialog(input.ReadStrings()),
                    ct),
            RunExclusiveOperationAsync = (operation, ct) =>
                input.OperationLocks.InstallExecutionLock.TryRunExclusiveAsync(operation, ct),
            ReadInstallManagementDialogText =
                () => InstallManagementDialogText.FromAppStrings(input.ReadStrings()),
            SetSettingsStatusText = input.SetSettingsStatusText,
            GetInstallUnavailableDuringAppUpdateMessage =
                () => input.ReadStrings().InstallUnavailableDuringAppUpdate,
            ShowInstallManagementDialogAsync = input.InstallManagementDialogService.ShowDialogAsync,
            HandleUninstallAsync = input.HandleUninstallAsync,
            LogInstallManagementDialogResult = (gameId, statusCode, action) =>
                input.AppLogger.Info(
                    MainViewModelLogCategories.UninstallUi,
                    $"popup result source=install_management game_id={gameId} status={statusCode} result={action}"),
            ExecuteCurrentInstallFlowAsync = input.ExecuteCurrentInstallFlowAsync
        };
    }

    private static MainUninstallInteractionCompositionInput CreateUninstallInteractionInput(
        MainInstallShellFacadeInput input,
        MainInstallContextFactorySharedCompositionServices shared)
    {
        return new MainUninstallInteractionCompositionInput
        {
            ReadSelectionState = input.ReadSelectionState,
            ResolveScannedTargetPathByGameId = gameId =>
                input.ScannedGameState.TryGetTargetPath(gameId, out var targetPath)
                    ? targetPath
                    : null,
            FlowRequestFactory = input.FlowRequestFactory,
            BusyActions = shared.BusyActions,
            SetSettingsStatusText = input.SetSettingsStatusText,
            ReadUninstallFlowText = () => UninstallFlowText.FromAppStrings(input.ReadStrings()),
            UninstallInteractionController = input.InstallDependencies.MainUninstallInteractionController,
            UninstallFlowCoordinator = input.InstallDependencies.UninstallFlowCoordinator,
            SelectionRefreshActions = shared.SelectionRefreshActions,
            LogUninstallFlowInfo = message =>
                input.AppLogger.Info(MainViewModelLogCategories.UninstallFlow, message),
            LogUninstallFlowWarning = message =>
                input.AppLogger.Warning(MainViewModelLogCategories.UninstallFlow, message),
            LogUninstallFlowError = (message, ex) =>
                input.AppLogger.Error(MainViewModelLogCategories.UninstallFlow, message, ex)
        };
    }

    private static MainInstallCompletionCompositionInput CreateInstallCompletionInput(
        MainInstallShellFacadeInput input,
        MainInstallContextFactorySharedCompositionServices shared)
    {
        return new MainInstallCompletionCompositionInput
        {
            ResolveSelectedGame = input.ResolveSelectedGame,
            SelectionRefreshActions = shared.SelectionRefreshActions,
            FlowLogActions = shared.FlowLogActions,
            CreateInstallStateUpdate = input.ResultApplier.CreateInstallStateUpdate,
            ApplyStateUpdate = input.ApplyStateUpdate,
            ApplyDeferredStateUpdate = input.ApplyDeferredStateUpdate,
            BuildCompletionDialog = update =>
                update.PopupRequest is null
                    ? null
                    : input.InstallDependencies.InstallPopupPresenter.BuildDialogRequest(
                        update.PopupRequest,
                        input.ReadStrings()),
            InstallCompletionController = input.InstallDependencies.MainInstallCompletionController,
            ShowCompletionDialogAsync = input.DialogPresenter.ShowSafelyAsync,
            OpenExternalUrl = input.ExternalUrlLauncher.OpenUrl,
            ClearSelectedGameContext = input.ClearSelectedGameContext,
            LogInstallInfo = message => input.AppLogger.Info(MainViewModelLogCategories.Install, message),
            LogInstallWarning = message => input.AppLogger.Warning(MainViewModelLogCategories.Install, message),
            LogInstallError = (message, ex) =>
                input.AppLogger.Error(MainViewModelLogCategories.Install, message, ex)
        };
    }

    private static MainInstallArchiveReadinessCompositionInput CreateInstallArchiveReadinessInput(
        MainInstallShellFacadeInput input,
        MainInstallContextFactorySharedCompositionServices shared)
    {
        return new MainInstallArchiveReadinessCompositionInput
        {
            FlowLogActions = shared.FlowLogActions,
            RuntimeSnapshotReaders = shared.RuntimeSnapshotReaders,
            ReadLatestOptiScalerVariantCatalog =
                () => input.RuntimeShellState.LatestOptiScalerVariantCatalog,
            ReadPreferredOptiScalerVariant = input.ReadPreferredOptiScalerVariant,
            ReadLatestFsr4VariantCatalog =
                () => input.RuntimeShellState.LatestFsr4VariantCatalog,
            ArchiveReadinessRefreshCoordinator = input.ArchiveReadinessRefreshCoordinator,
            ArchiveReadinessFlowController = input.InstallDependencies.ArchiveReadinessFlowController,
            SetArchiveReadiness = input.RuntimeShellState.SetArchiveReadiness,
            ApplyOptiScalerVariantSyncToRuntimeState =
                input.RuntimeShellState.ApplyOptiScalerVariantSync,
            ApplyOptiScalerVariantOptions = input.ApplyOptiScalerVariantOptions,
            PersistEffectiveVariantPreference = input.PersistEffectiveVariantPreference,
            SaveUserSettings = input.SaveUserSettings
        };
    }

    private static MainInstallPreparationCompositionInput CreateInstallPreparationInput(
        MainInstallShellFacadeInput input,
        MainInstallContextFactorySharedCompositionServices shared)
    {
        return new MainInstallPreparationCompositionInput
        {
            IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = input.IsAppUpdateInProgress,
            ResolveSelectedGame = input.ResolveSelectedGame,
            ReadSelectionState = input.ReadSelectionState,
            RuntimeSnapshotReaders = shared.RuntimeSnapshotReaders,
            FlowRequestFactory = input.FlowRequestFactory,
            ResolveSelectedIndex = input.ResolveSelectedIndex,
            ReadLatestRuntimeContext = () => input.RuntimeShellState.LatestRuntimeContext,
            ReadLatestArchiveReadiness = () => input.RuntimeShellState.LatestArchiveReadiness,
            IsOperatingSystemSupported = input.IsOperatingSystemSupported,
            ReadInstallFlowText = () => InstallFlowText.FromAppStrings(input.ReadStrings()),
            ReadLatestRemoteCatalogErrorCode =
                () => input.RuntimeShellState.LatestRemoteCatalogErrorCode,
            RefreshArchiveReadinessAsync = input.RefreshArchiveReadinessAsync,
            RefreshSelectionForInstallAsync = input.RefreshSelectionForInstallAsync,
            CreateOptiScalerIniApplyContext =
                input.InstallDependencies.MainOptiScalerSettingsController.CreateIniApplyContext
        };
    }

    private static MainInstallExecutionBridgeCompositionInput CreateInstallExecutionBridgeInput(
        MainInstallShellFacadeInput input,
        MainInstallContextFactorySharedCompositionServices shared,
        Func<MainInstallCompletionContext> createInstallCompletionContext)
    {
        return new MainInstallExecutionBridgeCompositionInput
        {
            InstallExecutionCoordinator = input.InstallDependencies.InstallExecutionCoordinator,
            MainInstallCompletionController = input.InstallDependencies.MainInstallCompletionController,
            BusyActions = shared.BusyActions,
            CreateInstallCompletionContext = createInstallCompletionContext,
            ReadInstallExecutionText = () => InstallExecutionCoordinatorText.FromAppStrings(input.ReadStrings())
        };
    }
}

internal sealed record MainInstallShellFacadeInput
{
    public required MainInstallResolvedDependencies InstallDependencies { get; init; }
    public required MainShellOperationLocks OperationLocks { get; init; }
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required DialogPresenter DialogPresenter { get; init; }
    public required StartupNoticePresenter StartupNoticePresenter { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IExternalUrlLauncher ExternalUrlLauncher { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RefreshSelectionForInstallAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> HandleUninstallAsync { get; init; }
    public required Func<CancellationToken, Task> ExecuteCurrentInstallFlowAsync { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Action ClearSelectedGameContext { get; init; }
    public required Func<string> ReadPreferredOptiScalerVariant { get; init; }
    public required Action ApplyOptiScalerVariantOptions { get; init; }
    public required Action<string> PersistEffectiveVariantPreference { get; init; }
    public required Action SaveUserSettings { get; init; }
    public required Func<bool> IsOperatingSystemSupported { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
}

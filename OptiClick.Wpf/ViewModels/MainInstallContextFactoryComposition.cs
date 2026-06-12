using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainInstallContextFactoryCompositionServices
{
    public required MainInstallInteractionContextFactory InstallInteraction { get; init; }
    public required MainUninstallInteractionContextFactory UninstallInteraction { get; init; }
    public required MainInstallCompletionContextFactory InstallCompletion { get; init; }
    public required MainInstallArchiveReadinessContextFactory InstallArchiveReadiness { get; init; }
    public required MainInstallPreparationContextFactory InstallPreparation { get; init; }
    public required MainInstallExecutionBridgeContextFactory InstallExecutionBridge { get; init; }
}

internal sealed record MainInstallBusyActions
{
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
}

internal sealed record MainInstallSelectionRefreshActions
{
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
}

internal sealed record MainInstallRuntimeSnapshotReaders
{
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
}

internal sealed record MainInstallFlowLogActions
{
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
}

internal sealed record MainInstallContextFactoryCompositionInput
{
    public required MainInstallInteractionCompositionInput InstallInteraction { get; init; }
    public required MainUninstallInteractionCompositionInput UninstallInteraction { get; init; }
    public required MainInstallCompletionCompositionInput InstallCompletion { get; init; }
    public required MainInstallArchiveReadinessCompositionInput InstallArchiveReadiness { get; init; }
    public required MainInstallPreparationCompositionInput InstallPreparation { get; init; }
    public required MainInstallExecutionBridgeCompositionInput InstallExecutionBridge { get; init; }
}

internal sealed record MainInstallInteractionCompositionInput
{
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<string> ReadSelectedInstallStatusCode { get; init; }
    public required Func<string> ReadSelectedGameId { get; init; }
    public required Func<CancellationToken, Task> ShowStartupBlockDialogAsync { get; init; }
    public required Func<Func<CancellationToken, Task>, CancellationToken, Task> RunExclusiveOperationAsync { get; init; }
    public required Func<InstallManagementDialogText> ReadInstallManagementDialogText { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<string> GetInstallUnavailableDuringAppUpdateMessage { get; init; }
    public required Func<InstallManagementDialogRequest, CancellationToken, Task<InstallManagementDialogResult>>
        ShowInstallManagementDialogAsync
    {
        get;
        init;
    }

    public required Func<GameCardViewModel, CancellationToken, Task> HandleUninstallAsync { get; init; }
    public required Action<string, string, InstallManagementDialogResult> LogInstallManagementDialogResult { get; init; }
    public required Func<CancellationToken, Task> ExecuteCurrentInstallFlowAsync { get; init; }
}

internal sealed record MainUninstallInteractionCompositionInput
{
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<string, string?> ResolveScannedTargetPathByGameId { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required MainInstallBusyActions BusyActions { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<UninstallFlowText> ReadUninstallFlowText { get; init; }
    public required MainUninstallInteractionController UninstallInteractionController { get; init; }
    public required UninstallFlowCoordinator UninstallFlowCoordinator { get; init; }
    public required MainInstallSelectionRefreshActions SelectionRefreshActions { get; init; }
    public required Action<string> LogUninstallFlowInfo { get; init; }
    public required Action<string> LogUninstallFlowWarning { get; init; }
    public required Action<string, Exception> LogUninstallFlowError { get; init; }
}

internal sealed record MainInstallCompletionCompositionInput
{
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required MainInstallSelectionRefreshActions SelectionRefreshActions { get; init; }
    public required MainInstallFlowLogActions FlowLogActions { get; init; }
    public required Func<InstallFlowResult, MainViewModelStateUpdate> CreateInstallStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Func<MainViewModelStateUpdate, AppDialogRequest?> BuildCompletionDialog { get; init; }
    public required MainInstallCompletionController InstallCompletionController { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowCompletionDialogAsync { get; init; }
    public required Action ClearSelectedGameContext { get; init; }
    public required Action<string> LogInstallInfo { get; init; }
    public required Action<string> LogInstallWarning { get; init; }
    public required Action<string, Exception> LogInstallError { get; init; }
}

internal sealed record MainInstallArchiveReadinessCompositionInput
{
    public required MainInstallFlowLogActions FlowLogActions { get; init; }
    public required MainInstallRuntimeSnapshotReaders RuntimeSnapshotReaders { get; init; }
    public required Func<OptiScalerVariantCatalog> ReadLatestOptiScalerVariantCatalog { get; init; }
    public required Func<string> ReadPreferredOptiScalerVariant { get; init; }
    public required Func<Fsr4VariantCatalog> ReadLatestFsr4VariantCatalog { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required Action<ArchiveReadinessSnapshot> SetArchiveReadiness { get; init; }
    public required Action<OptiScalerVariantSyncResult?> ApplyOptiScalerVariantSyncToRuntimeState { get; init; }
    public required Action ApplyOptiScalerVariantOptions { get; init; }
    public required Action<string> PersistEffectiveVariantPreference { get; init; }
    public required Action SaveUserSettings { get; init; }
}

internal sealed record MainInstallPreparationCompositionInput
{
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required MainInstallRuntimeSnapshotReaders RuntimeSnapshotReaders { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
    public required Func<RuntimeContext> ReadLatestRuntimeContext { get; init; }
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required Func<bool> IsOperatingSystemSupported { get; init; }
    public required Func<InstallFlowText> ReadInstallFlowText { get; init; }
    public required Func<string> ReadLatestRemoteCatalogErrorCode { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RefreshSelectionForInstallAsync { get; init; }
    public required Func<OptiScalerIniApplyContext> CreateOptiScalerIniApplyContext { get; init; }
}

internal sealed record MainInstallExecutionBridgeCompositionInput
{
    public required InstallExecutionCoordinator InstallExecutionCoordinator { get; init; }
    public required MainInstallCompletionController MainInstallCompletionController { get; init; }
    public required MainInstallBusyActions BusyActions { get; init; }
    public required Func<MainInstallCompletionContext> CreateInstallCompletionContext { get; init; }
    public required Func<InstallExecutionCoordinatorText> ReadInstallExecutionText { get; init; }
}

internal static class MainInstallContextFactoryComposition
{
    public static MainInstallContextFactoryCompositionServices Compose(
        MainInstallContextFactoryCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.InstallInteraction);
        ArgumentNullException.ThrowIfNull(input.UninstallInteraction);
        ArgumentNullException.ThrowIfNull(input.InstallCompletion);
        ArgumentNullException.ThrowIfNull(input.InstallArchiveReadiness);
        ArgumentNullException.ThrowIfNull(input.InstallPreparation);
        ArgumentNullException.ThrowIfNull(input.InstallExecutionBridge);

        var installInteraction = input.InstallInteraction;
        var uninstallInteraction = input.UninstallInteraction;
        var installCompletion = input.InstallCompletion;
        var installArchiveReadiness = input.InstallArchiveReadiness;
        var installPreparation = input.InstallPreparation;
        var installExecutionBridge = input.InstallExecutionBridge;

        ArgumentNullException.ThrowIfNull(uninstallInteraction.BusyActions);
        ArgumentNullException.ThrowIfNull(uninstallInteraction.SelectionRefreshActions);
        ArgumentNullException.ThrowIfNull(installCompletion.SelectionRefreshActions);
        ArgumentNullException.ThrowIfNull(installCompletion.FlowLogActions);
        ArgumentNullException.ThrowIfNull(installArchiveReadiness.FlowLogActions);
        ArgumentNullException.ThrowIfNull(installArchiveReadiness.RuntimeSnapshotReaders);
        ArgumentNullException.ThrowIfNull(installPreparation.RuntimeSnapshotReaders);
        ArgumentNullException.ThrowIfNull(installExecutionBridge.BusyActions);

        return new MainInstallContextFactoryCompositionServices
        {
            InstallInteraction = new MainInstallInteractionContextFactory(
                new MainInstallInteractionContextFactoryInput
                {
                    ShouldBlockStartupForUnsupportedOperatingSystem =
                        installInteraction.ShouldBlockStartupForUnsupportedOperatingSystem,
                    ResolveSelectedGame = installInteraction.ResolveSelectedGame,
                    IsAppUpdateInProgress = installInteraction.IsAppUpdateInProgress,
                    ReadSelectedInstallStatusCode = installInteraction.ReadSelectedInstallStatusCode,
                    ReadSelectedGameId = installInteraction.ReadSelectedGameId,
                    ShowStartupBlockDialogAsync = installInteraction.ShowStartupBlockDialogAsync,
                    RunExclusiveOperationAsync = installInteraction.RunExclusiveOperationAsync,
                    ReadInstallManagementDialogText = installInteraction.ReadInstallManagementDialogText,
                    SetSettingsStatusText = installInteraction.SetSettingsStatusText,
                    GetInstallUnavailableDuringAppUpdateMessage =
                        installInteraction.GetInstallUnavailableDuringAppUpdateMessage,
                    ShowInstallManagementDialogAsync = installInteraction.ShowInstallManagementDialogAsync,
                    HandleUninstallAsync = installInteraction.HandleUninstallAsync,
                    LogInstallManagementDialogResult = installInteraction.LogInstallManagementDialogResult,
                    ExecuteCurrentInstallFlowAsync = installInteraction.ExecuteCurrentInstallFlowAsync
                }),
            UninstallInteraction = new MainUninstallInteractionContextFactory(
                new MainUninstallInteractionContextFactoryInput
                {
                    ReadSelectionState = uninstallInteraction.ReadSelectionState,
                    ResolveScannedTargetPathByGameId = uninstallInteraction.ResolveScannedTargetPathByGameId,
                    FlowRequestFactory = uninstallInteraction.FlowRequestFactory,
                    ApplyInstallBusyState = uninstallInteraction.BusyActions.ApplyInstallBusyState,
                    SetSettingsStatusText = uninstallInteraction.SetSettingsStatusText,
                    ReadUninstallFlowText = uninstallInteraction.ReadUninstallFlowText,
                    UninstallInteractionController = uninstallInteraction.UninstallInteractionController,
                    UninstallFlowCoordinator = uninstallInteraction.UninstallFlowCoordinator,
                    ReadInstallButtonText = uninstallInteraction.SelectionRefreshActions.ReadInstallButtonText,
                    TryRefreshVisibleCard = uninstallInteraction.SelectionRefreshActions.TryRefreshVisibleCard,
                    SelectGameAsync = uninstallInteraction.SelectionRefreshActions.SelectGameAsync,
                    LogUninstallFlowInfo = uninstallInteraction.LogUninstallFlowInfo,
                    LogUninstallFlowWarning = uninstallInteraction.LogUninstallFlowWarning,
                    LogUninstallFlowError = uninstallInteraction.LogUninstallFlowError
                }),
            InstallCompletion = new MainInstallCompletionContextFactory(
                new MainInstallCompletionContextFactoryInput
                {
                    DispatchFlowLogs = installCompletion.FlowLogActions.DispatchFlowLogs,
                    CreateInstallStateUpdate = installCompletion.CreateInstallStateUpdate,
                    ApplyStateUpdate = installCompletion.ApplyStateUpdate,
                    ApplyDeferredStateUpdate = installCompletion.ApplyDeferredStateUpdate,
                    BuildCompletionDialog = installCompletion.BuildCompletionDialog,
                    InstallCompletionController = installCompletion.InstallCompletionController,
                    ShowCompletionDialogAsync = installCompletion.ShowCompletionDialogAsync,
                    ClearSelectedGameContext = installCompletion.ClearSelectedGameContext,
                    ReadSelectedGame = installCompletion.ResolveSelectedGame,
                    ReadInstallButtonText = installCompletion.SelectionRefreshActions.ReadInstallButtonText,
                    TryRefreshVisibleCard = installCompletion.SelectionRefreshActions.TryRefreshVisibleCard,
                    SelectGameAsync = installCompletion.SelectionRefreshActions.SelectGameAsync,
                    LogInstallInfo = installCompletion.LogInstallInfo,
                    LogInstallWarning = installCompletion.LogInstallWarning,
                    LogInstallError = installCompletion.LogInstallError
                }),
            InstallArchiveReadiness = new MainInstallArchiveReadinessContextFactory(
                new MainInstallArchiveReadinessContextFactoryInput
                {
                    ReadModuleDownloadLinks = installArchiveReadiness.RuntimeSnapshotReaders.ReadModuleDownloadLinks,
                    ReadLatestOptiScalerVariantCatalog = installArchiveReadiness.ReadLatestOptiScalerVariantCatalog,
                    ReadPreferredOptiScalerVariant = installArchiveReadiness.ReadPreferredOptiScalerVariant,
                    ReadLatestFsr4VariantCatalog = installArchiveReadiness.ReadLatestFsr4VariantCatalog,
                    ArchiveReadinessRefreshCoordinator = installArchiveReadiness.ArchiveReadinessRefreshCoordinator,
                    ArchiveReadinessFlowController = installArchiveReadiness.ArchiveReadinessFlowController,
                    DispatchFlowLogs = installArchiveReadiness.FlowLogActions.DispatchFlowLogs,
                    SetArchiveReadiness = installArchiveReadiness.SetArchiveReadiness,
                    ApplyOptiScalerVariantSyncToRuntimeState =
                        installArchiveReadiness.ApplyOptiScalerVariantSyncToRuntimeState,
                    ApplyOptiScalerVariantOptions = installArchiveReadiness.ApplyOptiScalerVariantOptions,
                    PersistEffectiveVariantPreference = installArchiveReadiness.PersistEffectiveVariantPreference,
                    SaveUserSettings = installArchiveReadiness.SaveUserSettings
                }),
            InstallPreparation = new MainInstallPreparationContextFactory(
                new MainInstallPreparationContextFactoryInput
                {
                    IsInstallExecutionInProgress = installPreparation.IsInstallExecutionInProgress,
                    IsAppUpdateInProgress = installPreparation.IsAppUpdateInProgress,
                    ResolveSelectedGame = installPreparation.ResolveSelectedGame,
                    ResolveSelectedIndex = installPreparation.ResolveSelectedIndex,
                    ReadLatestRuntimeContext = installPreparation.ReadLatestRuntimeContext,
                    ReadLatestArchiveReadiness = installPreparation.ReadLatestArchiveReadiness,
                    ReadSelectionState = installPreparation.ReadSelectionState,
                    ReadModuleDownloadLinks = installPreparation.RuntimeSnapshotReaders.ReadModuleDownloadLinks,
                    IsOperatingSystemSupported = installPreparation.IsOperatingSystemSupported,
                    ReadInstallFlowText = installPreparation.ReadInstallFlowText,
                    ReadLatestRemoteCatalogErrorCode = installPreparation.ReadLatestRemoteCatalogErrorCode,
                    RefreshArchiveReadinessAsync = installPreparation.RefreshArchiveReadinessAsync,
                    RefreshSelectionForInstallAsync = installPreparation.RefreshSelectionForInstallAsync,
                    CreateOptiScalerIniApplyContext = installPreparation.CreateOptiScalerIniApplyContext,
                    FlowRequestFactory = installPreparation.FlowRequestFactory
                }),
            InstallExecutionBridge = new MainInstallExecutionBridgeContextFactory(
                new MainInstallExecutionBridgeContextFactoryInput
                {
                    InstallExecutionCoordinator = installExecutionBridge.InstallExecutionCoordinator,
                    MainInstallCompletionController = installExecutionBridge.MainInstallCompletionController,
                    ApplyInstallBusyState = installExecutionBridge.BusyActions.ApplyInstallBusyState,
                    CreateInstallCompletionContext = installExecutionBridge.CreateInstallCompletionContext,
                    ReadInstallExecutionText = installExecutionBridge.ReadInstallExecutionText
                })
        };
    }
}

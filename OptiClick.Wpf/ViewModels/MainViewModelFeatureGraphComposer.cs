using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels.Features;
using OptiClick.Wpf.ViewModels.Features.Install;
using OptiClick.Wpf.ViewModels.Features.Runtime;
using OptiClick.Wpf.ViewModels.Features.Selection;
using OptiClick.Wpf.ViewModels.Features.Shell;
using OptiClick.Wpf.ViewModels.Features.Startup;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;
using OptiClick.Wpf.ViewModels.Sections;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainViewModelFeatureGraph
{
    public required MainShellFeatureFacades Features { get; init; }
    public required MainViewModelStateApplier StateApplier { get; init; }
    public required ShellSections Sections { get; init; }
}

internal sealed record MainShellFeatureFacades
{
    public required MainShellInteractionFeatureFacade ShellInteraction { get; init; }
    public required MainStartupFeatureFacade Startup { get; init; }
    public required MainInstallFeatureFacade Install { get; init; }
    public required MainSelectionFeatureFacade Selection { get; init; }
    public required MainRuntimeFeatureFacade Runtime { get; init; }
}

internal sealed record MainViewModelFeatureGraphCompositionInput
{
    public required MainViewModelCompositionDependencies Dependencies { get; init; }
    public required MainShellFacadePorts Ports { get; init; }
    public required MainViewModelFeatureFacadeRegistry Registry { get; init; }
    public required MainShellInteractionContextFactoryInput ShellInteractionContext { get; init; }
    public required MainFeatureGraphStateCallbacks State { get; init; }
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required MainShellOperationLocks OperationLocks { get; init; }
    public required MainOptiScalerSettingsController OptiScalerSettingsController { get; init; }
    public required bool SeedMockGameCards { get; init; }
    public required bool SeedMockScanFolders { get; init; }
    public required IReadOnlyList<string> SettingsLanguageOptions { get; init; }
    public required string InitialSettingsLanguageOption { get; init; }
}

internal sealed record MainFeatureGraphStateCallbacks
{
    public required Action<ScanFolderStateUpdate> ApplyScanFolderStateUpdate { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Action<string> SetScanStatusText { get; init; }
    public required Action<bool, bool, string?, string?> ApplyAppLog { get; init; }
    public required Func<Task> RefreshHomeCoversOnDispatcherAsync { get; init; }
}

internal sealed class MainViewModelFeatureFacadeRegistry
{
    private MainShellInteractionFeatureFacade? _shellInteractionFeature;
    private MainStartupFeatureFacade? _startupFeature;
    private MainInstallFeatureFacade? _installFeature;
    private MainSelectionFeatureFacade? _selectionFeature;
    private MainRuntimeFeatureFacade? _runtimeFeature;

    public MainShellInteractionFeatureFacade ShellInteractionFeature
    {
        get => _shellInteractionFeature ?? throw CreateMissingServiceException(nameof(ShellInteractionFeature));
        set => _shellInteractionFeature = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MainStartupFeatureFacade StartupFeature
    {
        get => _startupFeature ?? throw CreateMissingServiceException(nameof(StartupFeature));
        set => _startupFeature = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MainInstallFeatureFacade InstallFeature
    {
        get => _installFeature ?? throw CreateMissingServiceException(nameof(InstallFeature));
        set => _installFeature = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MainSelectionFeatureFacade SelectionFeature
    {
        get => _selectionFeature ?? throw CreateMissingServiceException(nameof(SelectionFeature));
        set => _selectionFeature = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MainRuntimeFeatureFacade RuntimeFeature
    {
        get => _runtimeFeature ?? throw CreateMissingServiceException(nameof(RuntimeFeature));
        set => _runtimeFeature = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static InvalidOperationException CreateMissingServiceException(string serviceName)
    {
        return new InvalidOperationException($"{serviceName} has not been composed.");
    }
}

internal static class MainViewModelFeatureGraphComposer
{
    public static MainViewModelFeatureGraph Compose(MainViewModelFeatureGraphCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var dependencies = input.Dependencies;
        var featureDependencies = dependencies.Features;
        var shellDependencies = dependencies.Shell;
        var startupDependencies = featureDependencies.Startup;
        var installDependencies = featureDependencies.Install;
        var selectionDependencies = featureDependencies.Selection;
        var scanDependencies = featureDependencies.Scan;
        var runtimeDependencies = featureDependencies.Runtime;
        var ports = input.Ports;

        var shellInteractionFeature = MainViewModelFeatureFacadeComposer.ComposeShellInteraction(
            new MainShellInteractionFeatureCompositionInput
            {
                AppDependencies = dependencies.App,
                ShellDependencies = shellDependencies,
                StartupDependencies = startupDependencies,
                SelectionDependencies = selectionDependencies,
                Context = input.ShellInteractionContext
            });
        input.Registry.ShellInteractionFeature = shellInteractionFeature;

        var startupShellFacade = MainViewModelShellFacadeComposer.ComposeStartup(
            new MainStartupShellFacadeCompositionInput
            {
                StartupDependencies = startupDependencies,
                Ports = ports
            });
        var startupFeature = MainViewModelFeatureFacadeComposer.ComposeStartup(
            new MainStartupFeatureCompositionInput
            {
                StartupDependencies = startupDependencies,
                StartupShellFacade = startupShellFacade,
                DialogsPort = new MainStartupDialogsPort
                {
                    ShowStartupAnnouncementIfNeededAsync =
                        shellInteractionFeature.ShowStartupAnnouncementIfNeededAsync,
                    ShowStartupUpdateCheckDialogAsync =
                        shellInteractionFeature.ShowStartupUpdateCheckDialogAsync,
                    UpdateStartupPreparationState = ports.Startup.UpdateStartupPreparationState,
                    ClearLastErrorCode = ports.Startup.ClearLastErrorCode,
                    LogWarning = message =>
                        ports.App.AppLogger.Warning(MainViewModelLogCategories.Startup, message)
                },
                CoverPrefetchPort = new MainStartupCoverPrefetchPort
                {
                    GameMasterAccessor = () => input.RuntimeShellState.LatestRuntimeData.GameMaster,
                    HomeCardsAccessor = ports.Selection.ReadVisibleCards,
                    RefreshHomeCoversOnDispatcherAsync = input.State.RefreshHomeCoversOnDispatcherAsync,
                    UpdateStartupPreparationState = ports.Startup.UpdateStartupPreparationState,
                    ClearLastErrorCode = ports.Startup.ClearLastErrorCode,
                    LogInfo = message => ports.App.AppLogger.Info(MainViewModelLogCategories.Wiki, message),
                    LogWarning = message => ports.App.AppLogger.Warning(MainViewModelLogCategories.Wiki, message)
                }
            });
        input.Registry.StartupFeature = startupFeature;

        var installShellFacade = MainViewModelShellFacadeComposer.ComposeInstall(
            new MainInstallShellFacadeCompositionInput
            {
                InstallDependencies = installDependencies,
                StartupDependencies = startupDependencies,
                Ports = ports
            });
        var installFeature = installShellFacade.Feature;
        input.Registry.InstallFeature = installFeature;

        var selectionShellFacade = MainViewModelShellFacadeComposer.ComposeSelection(
            new MainSelectionShellFacadeCompositionInput
            {
                SelectionDependencies = selectionDependencies,
                SelectionScanDependencies = featureDependencies.SelectionScan,
                Ports = ports
            });
        var selectionFeature = selectionShellFacade.Feature;
        input.Registry.SelectionFeature = selectionFeature;

        var scanShellFacade = MainViewModelShellFacadeComposer.ComposeScan(
            new MainScanShellFacadeCompositionInput
            {
                ScanDependencies = scanDependencies,
                Ports = ports
            });

        var stateApplier = CreateStateApplier(
            input,
            shellDependencies,
            shellInteractionFeature,
            installFeature);

        var runtimeShellFacade = MainViewModelShellFacadeComposer.ComposeRuntime(
            new MainRuntimeShellFacadeCompositionInput
            {
                RuntimeDependencies = runtimeDependencies,
                RuntimeFlowDependencies = featureDependencies.RuntimeFlow,
                Ports = ports
            });
        var runtimeFeature = MainViewModelFeatureFacadeComposer.ComposeRuntime(
            new MainRuntimeFeatureCompositionInput
            {
                RuntimeDependencies = runtimeDependencies,
                StartupDependencies = startupDependencies,
                RuntimeShellFacade = runtimeShellFacade,
                RuntimeShellState = input.RuntimeShellState,
                OperationLocks = input.OperationLocks,
                FlowLogDispatcher = shellDependencies.FlowLogDispatcher,
                BuildCatalogRefreshRequest = () => new MainRuntimeDataCatalogRefreshRequest
                {
                    SelectedLanguage = ports.Localization.ReadSelectedLanguage(),
                    Strings = ports.App.ReadStrings(),
                    BuildRuntimeCatalogRequest = ports.App.FlowRequestFactory.BuildRuntimeCatalogRequest,
                    ApplySettingsStatusText = ports.App.SetSettingsStatusText
                }
            });
        input.Registry.RuntimeFeature = runtimeFeature;

        var sections = MainViewModelShellSectionsComposer.Compose(
            new MainViewModelShellSectionsCompositionInput
            {
                Dependencies = featureDependencies.ShellSections,
                Ports = ports,
                ScanShellFacade = scanShellFacade,
                OptiScalerSettingsController = input.OptiScalerSettingsController,
                SeedMockGameCards = input.SeedMockGameCards,
                SeedMockScanFolders = input.SeedMockScanFolders,
                SettingsLanguageOptions = input.SettingsLanguageOptions,
                InitialSettingsLanguageOption = input.InitialSettingsLanguageOption
            });

        return new MainViewModelFeatureGraph
        {
            Features = new MainShellFeatureFacades
            {
                ShellInteraction = shellInteractionFeature,
                Startup = startupFeature,
                Install = installFeature,
                Selection = selectionFeature,
                Runtime = runtimeFeature
            },
            StateApplier = stateApplier,
            Sections = sections
        };
    }

    private static MainViewModelStateApplier CreateStateApplier(
        MainViewModelFeatureGraphCompositionInput input,
        MainShellResolvedDependencies shellDependencies,
        MainShellInteractionFeatureFacade shellInteractionFeature,
        MainInstallFeatureFacade installFeature)
    {
        return new MainViewModelStateApplier(
            applyScanFolderStateUpdate: input.State.ApplyScanFolderStateUpdate,
            setSettingsStatusText: message => input.State.SetSettingsStatusText(message ?? ""),
            setScanStatusText: message => input.State.SetScanStatusText(message ?? ""),
            queuePendingStartupNotice: shellInteractionFeature.QueuePendingStartupNotice,
            setRemoteCatalogError: (errorCode, detailErrorCode) =>
                input.RuntimeShellState.SetRemoteCatalogError(errorCode ?? "", detailErrorCode ?? ""),
            applyRuntimeData: (runtimeData, remoteCatalog, moduleDownloadLinks, variantCatalog, fsr4VariantCatalog) =>
                input.RuntimeShellState.ApplyRemoteCatalog(
                    runtimeData,
                    remoteCatalog,
                    moduleDownloadLinks,
                    variantCatalog,
                    fsr4VariantCatalog),
            replaceMatchByGameId: matchByGameId => input.ScannedGameState.ReplaceMatches(matchByGameId),
            replaceTargetPathByGameId: targetPathByGameId =>
                input.ScannedGameState.ReplaceTargetPaths(targetPathByGameId),
            replaceVisibleGames: visibleGames => input.Ports.Selection.ReplaceGameCards(visibleGames, true),
            writeAppLog: input.State.ApplyAppLog,
            showDeferredPopup: popup => shellDependencies.DialogPresenter.ShowDeferred(
                installFeature.BuildDialogRequest(popup, input.Ports.App.ReadStrings())),
            showDeferredDialog: shellDependencies.DialogPresenter.ShowDeferred,
            dispatchFlowLogs: (flowLogs, category) =>
                shellDependencies.FlowLogDispatcher.Dispatch(flowLogs, category));
    }
}

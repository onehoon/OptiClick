using System.Collections.ObjectModel;

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.ViewModels.Features.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed record MainSelectionShellFacade
{
    public required MainSelectionFeatureFacade Feature { get; init; }

    public static MainSelectionShellFacade Create(MainSelectionShellFacadeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SelectionDependencies);
        ArgumentNullException.ThrowIfNull(input.SelectionScanDependencies);

        return new MainSelectionShellFacade
        {
            Feature = new MainSelectionFeatureFacade(
                input.SelectionDependencies.MainLanguageChangeController,
                new MainLanguageChangeContextFactory(
                    new MainLanguageChangeContextFactoryInput
                    {
                        SetLanguage = input.SetLanguage,
                        RefreshLocalizedStrings = input.RefreshLocalizedStrings,
                        ApplySelectedGameLocalization = input.ApplySelectedGameLocalization,
                        RefreshSupportedGamesAfterLanguageChange =
                            input.RefreshSupportedGamesAfterLanguageChange,
                        BuildRefreshState = input.BuildRefreshState,
                        RefreshRuntimeContextAsync = input.RefreshRuntimeContextAsync,
                        RecomputeSelectionAfterScanAsync = input.RecomputeSelectionAfterScanAsync,
                        RefreshVisibleGamesAfterLanguageChangeAsync =
                            input.RefreshVisibleGamesAfterLanguageChangeAsync,
                        ReadStrings = input.ReadStrings,
                        LogInfo = input.LogLanguageChangeInfo,
                        LogWarning = input.LogLanguageChangeWarning,
                        ApplyLocalizationStateUpdate = input.ApplyLocalizationStateUpdate
                    }),
                input.SelectionDependencies.MainSelectionInteractionController,
                input.SelectionDependencies.MainSelectionRecomputeController,
                new MainSelectionScanContextFactory(
                    new MainSelectionScanContextFactoryInput
                    {
                        Dependencies = input.SelectionScanDependencies,
                        FlowRequestFactory = input.FlowRequestFactory,
                        Runtime = MainSelectionScanPortFactory.CreateRuntimePort(input.RuntimeShellState),
                        ScannedGames = MainSelectionScanPortFactory.CreateScannedGamePort(input.ScannedGameState),
                        ReadStrings = input.ReadStrings,
                        ReadSelectedLanguage = input.ReadSelectedLanguage,
                        ReadVisibleCards = input.ReadVisibleCards,
                        ReadSelectedGame = input.ReadSelectedGame,
                        SetSelectedGame = input.SetSelectedGame,
                        IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
                        IsAppUpdateInProgress = input.IsAppUpdateInProgress,
                        ReadSuppressHomeNavigationForAutoSelection =
                            input.ReadSuppressHomeNavigationForAutoSelection,
                        IncrementSelectionVersion = input.IncrementSelectionVersion,
                        ReadSelectionState = input.ReadSelectionState,
                        ReadSelectionVersion = input.ReadSelectionVersion,
                        ApplySelectionState = input.ApplySelectionState,
                        ApplySelectionBridgeState = input.ApplySelectionBridgeState,
                        ApplyPrecheckRunningIntermediate = input.ApplyPrecheckRunningIntermediate,
                        SetCurrentView = input.SetCurrentView,
                        QueueHomeCoverPrefetchInBackground = input.QueueHomeCoverPrefetchInBackground,
                        SelectGameAsync = input.SelectGameAsync,
                        DispatchSelectionLogs = logs =>
                            input.FlowLogDispatcher.Dispatch(logs, MainViewModelLogCategories.Selection),
                        LogAutoSelectionError = ex => input.AppLogger.Error(
                            MainViewModelLogCategories.Selection,
                            "auto selection failed while replacing game cards",
                            ex),
                        LogCreateVisibleCardError = ex => input.AppLogger.Error(
                            MainViewModelLogCategories.Scan,
                            "failed to create visible card from scan match",
                            ex),
                        LogCreateVisibleCardsError = ex => input.AppLogger.Error(
                            MainViewModelLogCategories.Scan,
                            "failed to create visible cards from scan matches",
                            ex)
                    }),
                input.SelectionDependencies.MainVisibleGameCardRefreshController,
                input.SelectionDependencies.GameCardSelectionStateController)
        };
    }
}

internal sealed record MainSelectionShellFacadeInput
{
    public required MainSelectionResolvedDependencies SelectionDependencies { get; init; }
    public required MainSelectionScanResolvedDependencies SelectionScanDependencies { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required RuntimeShellState RuntimeShellState { get; init; }
    public required ScannedGameState ScannedGameState { get; init; }
    public required FlowLogDispatcher FlowLogDispatcher { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Action<AppLanguage> SetLanguage { get; init; }
    public required Action RefreshLocalizedStrings { get; init; }
    public required Action ApplySelectedGameLocalization { get; init; }
    public required Action RefreshSupportedGamesAfterLanguageChange { get; init; }
    public required Func<AppLanguage, AppStrings, LocalizationStateUpdate> BuildRefreshState { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<CancellationToken, Task<bool>> RefreshVisibleGamesAfterLanguageChangeAsync { get; init; }
    public required Action<string> LogLanguageChangeInfo { get; init; }
    public required Action<string, Exception> LogLanguageChangeWarning { get; init; }
    public required Action<LocalizationStateUpdate> ApplyLocalizationStateUpdate { get; init; }
    public required Func<ObservableCollection<GameCardViewModel>> ReadVisibleCards { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Action<GameCardViewModel?> SetSelectedGame { get; init; }
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<bool> ReadSuppressHomeNavigationForAutoSelection { get; init; }
    public required Func<long> IncrementSelectionVersion { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<long> ReadSelectionVersion { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionState { get; init; }
    public required Action<ShellInstallSelectionState> ApplySelectionBridgeState { get; init; }
    public required Action ApplyPrecheckRunningIntermediate { get; init; }
    public required Action<ShellViewKind> SetCurrentView { get; init; }
    public required Action<string> QueueHomeCoverPrefetchInBackground { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
}

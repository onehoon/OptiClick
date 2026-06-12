using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Flow;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallCompletionContextFactory
{
    private readonly MainInstallCompletionContextFactoryInput _input;

    public MainInstallCompletionContextFactory(MainInstallCompletionContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainInstallCompletionContext Create()
    {
        return new MainInstallCompletionContext
        {
            Services = new MainInstallCompletionServices
            {
                DispatchFlowLogs = _input.DispatchFlowLogs,
                CreateInstallStateUpdate = _input.CreateInstallStateUpdate,
                ApplyStateUpdate = _input.ApplyStateUpdate,
                ApplyDeferredStateUpdate = _input.ApplyDeferredStateUpdate,
                BuildCompletionDialog = _input.BuildCompletionDialog,
                RefreshSelectionAfterSuccessfulInstallAsync = ct =>
                    _input.InstallCompletionController.RefreshSelectionAfterSuccessfulInstallAsync(
                        CreateSelectionRefreshContext(),
                        ct),
                ShowCompletionDialogAsync = _input.ShowCompletionDialogAsync,
                ClearSelectedGameContext = _input.ClearSelectedGameContext,
                LogInfo = _input.LogInstallInfo
            }
        };
    }

    private MainInstallCompletionSelectionRefreshContext CreateSelectionRefreshContext()
    {
        return new MainInstallCompletionSelectionRefreshContext
        {
            State = new MainInstallCompletionSelectionRefreshState
            {
                ReadSelectedGame = _input.ReadSelectedGame,
                ReadSelectedGameId = game => game?.ResolvedGameId ?? "",
                ReadInstallButtonText = _input.ReadInstallButtonText
            },
            Services = new MainInstallCompletionSelectionRefreshServices
            {
                TryRefreshVisibleCard = _input.TryRefreshVisibleCard,
                SelectGameAsync = _input.SelectGameAsync,
                LogInfo = _input.LogInstallInfo,
                LogWarn = _input.LogInstallWarning,
                LogError = _input.LogInstallError
            }
        };
    }
}

internal sealed record MainInstallCompletionContextFactoryInput
{
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Func<InstallFlowResult, MainViewModelStateUpdate> CreateInstallStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Func<MainViewModelStateUpdate, AppDialogRequest?> BuildCompletionDialog { get; init; }
    public required MainInstallCompletionController InstallCompletionController { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowCompletionDialogAsync { get; init; }
    public required Action ClearSelectedGameContext { get; init; }
    public required Func<GameCardViewModel?> ReadSelectedGame { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Action<string> LogInstallInfo { get; init; }
    public required Action<string> LogInstallWarning { get; init; }
    public required Action<string, Exception> LogInstallError { get; init; }
}

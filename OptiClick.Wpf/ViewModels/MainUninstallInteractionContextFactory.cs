using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainUninstallInteractionContextFactory
{
    private readonly MainUninstallInteractionContextFactoryInput _input;

    public MainUninstallInteractionContextFactory(MainUninstallInteractionContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainUninstallInteractionContext CreateInteractionContext(GameCardViewModel selectedGame)
    {
        var selectionStateBeforeExecution = _input.ReadSelectionState();
        return new MainUninstallInteractionContext
        {
            State = new MainUninstallInteractionState
            {
                ReadSelectionState = () => selectionStateBeforeExecution,
                ResolveShellGame = ShellGameCardMapper.Map,
                ResolveSelectedGameTargetPath = game =>
                    ResolveSelectedGameTargetPath(game, selectionStateBeforeExecution)
            },
            Services = new MainUninstallInteractionServices
            {
                BuildUninstallRequest = (selectedShellGame, targetPath, selectionSnapshot) =>
                    _input.FlowRequestFactory.BuildUninstallRequest(
                        selectedShellGame,
                        targetPath,
                        selectionSnapshot,
                        selectionStateBeforeExecution.ResolvedInputs,
                        new UninstallFlowCoordinatorUiActions
                        {
                            ApplyInstallBusyState = (inProgress, message) => _input.ApplyInstallBusyState(
                                inProgress,
                                message,
                                inProgress ? null : selectionStateBeforeExecution),
                            ApplySettingsStatusText = _input.SetSettingsStatusText,
                            RefreshSelectionAfterUninstallAsync = ct =>
                                _input.UninstallInteractionController.RefreshSelectionAfterUninstallAsync(
                                    CreateSelectionRefreshContext(),
                                    selectedGame,
                                    ct)
                        },
                        _input.ReadUninstallFlowText()),
                RunUninstallFlowAsync = _input.UninstallFlowCoordinator.RunAsync
            }
        };
    }

    private MainUninstallSelectionRefreshContext CreateSelectionRefreshContext()
    {
        return new MainUninstallSelectionRefreshContext
        {
            State = new MainUninstallSelectionRefreshState
            {
                ReadSelectedGameId = game => game?.ResolvedGameId ?? "",
                ReadInstallButtonText = _input.ReadInstallButtonText
            },
            Services = new MainUninstallSelectionRefreshServices
            {
                TryRefreshVisibleCard = _input.TryRefreshVisibleCard,
                SelectGameAsync = _input.SelectGameAsync,
                LogSuccess = _input.LogUninstallFlowInfo,
                LogCancel = () => _input.LogUninstallFlowWarning("uninstall status refresh result=canceled"),
                LogFailure = _input.LogUninstallFlowError
            }
        };
    }

    private string ResolveSelectedGameTargetPath(
        GameCardViewModel selectedGame,
        ShellInstallSelectionState selectionStateBeforeExecution)
    {
        var gameId = selectedGame?.ResolvedGameId ?? "";
        var scannedPath = string.IsNullOrWhiteSpace(gameId)
            ? null
            : _input.ResolveScannedTargetPathByGameId(gameId);
        if (!string.IsNullOrWhiteSpace(scannedPath))
        {
            return InstallTargetPathNormalizer.NormalizeTargetDirectory(scannedPath);
        }

        var fallbackPath = selectionStateBeforeExecution.SelectedMatchResult?.FolderPath;
        return InstallTargetPathNormalizer.NormalizeTargetDirectory(fallbackPath);
    }
}

internal sealed record MainUninstallInteractionContextFactoryInput
{
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<string, string?> ResolveScannedTargetPathByGameId { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<UninstallFlowText> ReadUninstallFlowText { get; init; }
    public required MainUninstallInteractionController UninstallInteractionController { get; init; }
    public required UninstallFlowCoordinator UninstallFlowCoordinator { get; init; }
    public required Func<string> ReadInstallButtonText { get; init; }
    public required Func<string, GameCardViewModel?> TryRefreshVisibleCard { get; init; }
    public required Func<GameCardViewModel?, CancellationToken, bool, bool, Task> SelectGameAsync { get; init; }
    public required Action<string> LogUninstallFlowInfo { get; init; }
    public required Action<string> LogUninstallFlowWarning { get; init; }
    public required Action<string, Exception> LogUninstallFlowError { get; init; }
}

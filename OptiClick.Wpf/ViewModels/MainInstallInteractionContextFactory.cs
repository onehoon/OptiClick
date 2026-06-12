using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallInteractionContextFactory
{
    private readonly MainInstallInteractionContextFactoryInput _input;

    public MainInstallInteractionContextFactory(MainInstallInteractionContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainInstallInteractionContext Create()
    {
        return new MainInstallInteractionContext
        {
            State = new MainInstallInteractionState
            {
                ShouldBlockStartupForUnsupportedOperatingSystem =
                    _input.ShouldBlockStartupForUnsupportedOperatingSystem,
                ResolveSelectedGame = _input.ResolveSelectedGame,
                IsAppUpdateInProgress = _input.IsAppUpdateInProgress,
                ReadSelectedInstallStatusCode = _input.ReadSelectedInstallStatusCode,
                ReadSelectedGameId = _input.ReadSelectedGameId
            },
            Services = new MainInstallInteractionServices
            {
                ShowStartupBlockDialogAsync = _input.ShowStartupBlockDialogAsync,
                RunExclusiveOperationAsync = _input.RunExclusiveOperationAsync
            },
            Callbacks = new MainInstallInteractionCallbacks
            {
                ReadInstallManagementDialogText = _input.ReadInstallManagementDialogText,
                SetSettingsStatusText = _input.SetSettingsStatusText,
                GetInstallUnavailableDuringAppUpdateMessage =
                    _input.GetInstallUnavailableDuringAppUpdateMessage,
                ShowInstallManagementDialogAsync = _input.ShowInstallManagementDialogAsync,
                HandleUninstallAsync = _input.HandleUninstallAsync,
                LogInstallManagementDialogResult = _input.LogInstallManagementDialogResult,
                ExecuteCurrentInstallFlowAsync = _input.ExecuteCurrentInstallFlowAsync
            }
        };
    }
}

internal sealed record MainInstallInteractionContextFactoryInput
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

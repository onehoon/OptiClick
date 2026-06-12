using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallInteractionState
{
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<string> ReadSelectedInstallStatusCode { get; init; }
    public required Func<string> ReadSelectedGameId { get; init; }
}

internal sealed class MainInstallInteractionServices
{
    public required Func<CancellationToken, Task> ShowStartupBlockDialogAsync { get; init; }
    public required Func<Func<CancellationToken, Task>, CancellationToken, Task> RunExclusiveOperationAsync { get; init; }
}

internal sealed class MainInstallInteractionCallbacks
{
    public required Func<InstallManagementDialogText> ReadInstallManagementDialogText { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<string> GetInstallUnavailableDuringAppUpdateMessage { get; init; }
    public required Func<InstallManagementDialogRequest, CancellationToken, Task<InstallManagementDialogResult>> ShowInstallManagementDialogAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> HandleUninstallAsync { get; init; }
    public required Action<string, string, InstallManagementDialogResult> LogInstallManagementDialogResult { get; init; }
    public required Func<CancellationToken, Task> ExecuteCurrentInstallFlowAsync { get; init; }
}

internal sealed class MainInstallInteractionController
{
    public async Task ShowInstallDialogAsync(
        MainInstallInteractionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.State.ShouldBlockStartupForUnsupportedOperatingSystem())
        {
            await context.Services.ShowStartupBlockDialogAsync(cancellationToken);
            return;
        }

        var selectedGame = context.State.ResolveSelectedGame();
        if (selectedGame is null)
        {
            return;
        }

        if (context.State.IsAppUpdateInProgress())
        {
            context.Callbacks.SetSettingsStatusText(context.Callbacks.GetInstallUnavailableDuringAppUpdateMessage());
            return;
        }

        await context.Services.RunExclusiveOperationAsync(
            async ct =>
            {
                if (await ResolveInstalledGameActionAsync(context, ct))
                {
                    return;
                }

                await context.Callbacks.ExecuteCurrentInstallFlowAsync(ct);
            },
            cancellationToken);
    }

    public async Task<bool> ResolveInstalledGameActionAsync(
        MainInstallInteractionContext context,
        CancellationToken cancellationToken = default)
    {
        var selectedGame = context.State.ResolveSelectedGame();
        if (selectedGame is null)
        {
            return false;
        }

        var statusCode = NormalizeStatusCode(
            context.State.ReadSelectedInstallStatusCode(),
            InstallStatusCodes.Installable);
        if (!IsInstalledSelectionStatus(statusCode))
        {
            return false;
        }

        var text = context.Callbacks.ReadInstallManagementDialogText();
        var request = BuildInstallManagementDialogRequest(statusCode, text);
        var selectedGameId = NormalizeStatusCode(context.State.ReadSelectedGameId(), "none");
        var action = await context.Callbacks.ShowInstallManagementDialogAsync(request, cancellationToken);
        context.Callbacks.LogInstallManagementDialogResult(
            selectedGameId,
            statusCode,
            action);

        if (action == InstallManagementDialogResult.Cancel)
        {
            return true;
        }

        if (action == InstallManagementDialogResult.ContinueInstall)
        {
            await context.Callbacks.ExecuteCurrentInstallFlowAsync(cancellationToken);
            return true;
        }

        await context.Callbacks.HandleUninstallAsync(selectedGame, cancellationToken);
        return true;
    }

    private static bool IsInstalledSelectionStatus(string statusCode)
    {
        var normalized = NormalizeStatusCode(statusCode, InstallStatusCodes.Installable);
        return !string.Equals(normalized, InstallStatusCodes.Installable, StringComparison.OrdinalIgnoreCase);
    }

    private static InstallManagementDialogRequest BuildInstallManagementDialogRequest(
        string statusCode,
        InstallManagementDialogText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var isUpdateAvailable = string.Equals(
            NormalizeStatusCode(statusCode, InstallStatusCodes.Installable),
            InstallStatusCodes.UpdateAvailable,
            StringComparison.OrdinalIgnoreCase);
        var message = isUpdateAvailable
            ? $"{text.UpdateAvailableSummary}\n{text.ChooseAction}"
            : $"{text.InstalledSummary}\n{text.ChooseAction}";

        return new InstallManagementDialogRequest
        {
            Title = text.DialogTitle,
            Message = message,
            DestructiveText = text.UninstallButton,
            PrimaryText = isUpdateAvailable ? text.UpdateButton : text.ReinstallButton,
            CancelText = text.CancelButton
        };
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

internal sealed class MainInstallInteractionContext
{
    public required MainInstallInteractionState State { get; init; }
    public required MainInstallInteractionServices Services { get; init; }
    public required MainInstallInteractionCallbacks Callbacks { get; init; }
}

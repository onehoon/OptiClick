using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainStartupDialogsController
{
    public async Task ShowRemoteCatalogDialogOnceAsync(
        MainRemoteCatalogDialogContext context,
        AppDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var normalizedCode = NormalizeStatusCode(request.ErrorCode, context.FallbackErrorCode);
        if (!context.DialogGate.TryMarkShown(normalizedCode, context.FallbackErrorCode))
        {
            return;
        }

        var normalizedRequest = request with
        {
            ErrorCode = normalizedCode
        };
        var result = await context.ShowDialogAsync(
            normalizedRequest,
            cancellationToken);
        if (context.HandleDialogResultAsync is not null)
        {
            await context.HandleDialogResultAsync(
                normalizedRequest,
                result,
                cancellationToken);
        }
    }

    public void StartInBackground(MainStartupDialogsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationTokenSource = context.Services.StartupBackgroundTaskManager.CreateSource();
        _ = RunInBackgroundAsync(context, cancellationTokenSource);
    }

    private static async Task RunInBackgroundAsync(
        MainStartupDialogsContext context,
        CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var canceled = false;
        var failed = false;
        try
        {
            await context.Services.ShowStartupAnnouncementIfNeededAsync(cancellationToken);
            await context.Services.ShowStartupUpdateCheckDialogAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            canceled = true;
            context.Callbacks.LogWarning("startup dialogs skipped reason=canceled");
        }
        catch (Exception ex)
        {
            failed = true;
            context.Callbacks.LogWarning($"startup dialogs failed type={ex.GetType().Name}");
        }
        finally
        {
            context.Callbacks.UpdateStartupPreparationState(state => state with
            {
                StartupDialogsRunning = false,
                StartupDialogsCompleted = !canceled && !failed,
                StartupDialogsCanceled = canceled,
                StartupDialogsFailed = failed,
                LastErrorCode = failed
                    ? "startup_dialogs_failed"
                    : !canceled
                        ? context.Callbacks.ClearLastErrorCode(state.LastErrorCode, "startup_dialogs_failed")
                        : state.LastErrorCode
            });
            context.Services.StartupBackgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

internal sealed class MainRemoteCatalogDialogContext
{
    public required OnceDialogGate DialogGate { get; init; }
    public required string FallbackErrorCode { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public Func<AppDialogRequest, AppDialogResult, CancellationToken, Task>? HandleDialogResultAsync { get; init; }
}

internal sealed class MainStartupDialogsContext
{
    public required MainStartupDialogsServices Services { get; init; }
    public required MainStartupDialogsCallbacks Callbacks { get; init; }
}

internal sealed class MainStartupDialogsServices
{
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required Func<CancellationToken, Task> ShowStartupAnnouncementIfNeededAsync { get; init; }
    public required Func<CancellationToken, Task> ShowStartupUpdateCheckDialogAsync { get; init; }
}

internal sealed class MainStartupDialogsCallbacks
{
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState
    { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<string> LogWarning { get; init; }
}

using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;

namespace OptiClick.Wpf.ViewModels.Features.Shell.ShellCommand;

internal sealed class MainShellCommandInteractionFeature
{
    private readonly ShellCommandActionController _controller;
    private readonly MainShellInteractionContextFactory _contextFactory;
    private bool _pendingAdministratorRelaunchCancelledNotice;

    public MainShellCommandInteractionFeature(
        ShellCommandActionController controller,
        MainShellInteractionContextFactory contextFactory)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public void OpenSupportRequest()
    {
        var context = _contextFactory.CreateShellCommandContext();
        ApplyShellCommandActionResult(
            context,
            _controller.OpenSupportRequest(
                context.CurrentAppVersion,
                context.RuntimeContext,
                context.SelectedLanguage,
                context.Strings));
    }

    public void OpenGameSupportRequest()
    {
        var context = _contextFactory.CreateShellCommandContext();
        ApplyShellCommandActionResult(
            context,
            _controller.OpenGameSupportRequest(
                context.CurrentAppVersion,
                context.RuntimeContext,
                context.SelectedLanguage,
                context.Strings));
    }

    public void OpenLogFolder()
    {
        var context = _contextFactory.CreateShellCommandContext();
        ApplyShellCommandActionResult(
            context,
            _controller.OpenLogFolder(context.LogDirectory, context.Strings));
    }

    public void NotifyAdministratorRelaunchCancelled()
    {
        var context = _contextFactory.CreateShellCommandContext();
        ApplyShellCommandActionResult(
            context,
            _controller.BuildAdministratorRelaunchCancelledNotice(context.Strings));
    }

    public void QueuePendingStartupNotice()
    {
        _pendingAdministratorRelaunchCancelledNotice = true;
    }

    public async Task ShowPendingStartupNoticesAsync(CancellationToken cancellationToken = default)
    {
        if (!_pendingAdministratorRelaunchCancelledNotice)
        {
            return;
        }

        _pendingAdministratorRelaunchCancelledNotice = false;
        var context = _contextFactory.CreateShellCommandContext();
        await context.ShowDialogAsync(
            _controller.BuildAdministratorRelaunchCancelledDialog(context.Strings),
            cancellationToken);
    }

    private static void ApplyShellCommandActionResult(
        MainShellCommandInteractionContext context,
        ShellCommandActionResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var update = context.ResultApplier.CreateShellCommandStateUpdate(result);
        context.ApplyStateUpdate(update);

        if (result.SupportActionResult is not null)
        {
            context.ApplyDeferredStateUpdate(
                context.ResultApplier.CreateSupportActionStateUpdate(result.SupportActionResult));
        }

        if (update.DialogRequest is not null)
        {
            context.ShowDeferredDialog(update.DialogRequest);
        }

        context.ApplyAppLog(
            result.ShouldWriteLog,
            result.LogAsWarning,
            result.LogCategory,
            result.LogMessage);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallExecutionBridge
{
    public async Task ExecuteAsync(
        MainInstallExecutionBridgeContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Preparation);

        var coordinatorResult = await context.Services.InstallExecutionCoordinator.RunAsync(
            new InstallExecutionCoordinatorRequest
            {
                FlowRequest = context.Preparation.Request,
                OverlaySnapshot = InstallExecutionOverlaySnapshot.FromSelectionState(
                    context.Preparation.SelectionStateBeforeExecution),
                Text = context.Callbacks.ReadInstallExecutionText(),
                ApplyInstallBusyState = (inProgress, message) =>
                    context.Callbacks.ApplyInstallBusyState(
                        inProgress,
                        message,
                        inProgress ? null : context.Preparation.SelectionStateBeforeExecution)
            },
            cancellationToken);

        await context.Services.MainInstallCompletionController.ApplyInstallExecutionResultAsync(
            context.Callbacks.CreateInstallCompletionContext(),
            coordinatorResult.Result,
            cancellationToken);
    }
}

internal sealed class MainInstallExecutionBridgeContext
{
    public required MainInstallExecutionPreparation Preparation { get; init; }
    public required MainInstallExecutionBridgeServices Services { get; init; }
    public required MainInstallExecutionBridgeCallbacks Callbacks { get; init; }
}

internal sealed class MainInstallExecutionBridgeServices
{
    public required InstallExecutionCoordinator InstallExecutionCoordinator { get; init; }
    public required MainInstallCompletionController MainInstallCompletionController { get; init; }
}

internal sealed class MainInstallExecutionBridgeCallbacks
{
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Func<MainInstallCompletionContext> CreateInstallCompletionContext { get; init; }
    public required Func<InstallExecutionCoordinatorText> ReadInstallExecutionText { get; init; }
}

using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Selection;
using System.Diagnostics;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallExecutionCoordinator
{
    private readonly Func<InstallFlowRequest, CancellationToken, Task<InstallFlowResult>> _executeInstallFlowAsync;

    public InstallExecutionCoordinator(InstallFlowController installFlowController)
        : this(CreateExecuteInstallFlowAsync(installFlowController))
    {
    }

    internal InstallExecutionCoordinator(Func<InstallFlowRequest, CancellationToken, Task<InstallFlowResult>> executeInstallFlowAsync)
    {
        _executeInstallFlowAsync = executeInstallFlowAsync ?? throw new ArgumentNullException(nameof(executeInstallFlowAsync));
    }

    private static Func<InstallFlowRequest, CancellationToken, Task<InstallFlowResult>> CreateExecuteInstallFlowAsync(
        InstallFlowController installFlowController)
    {
        ArgumentNullException.ThrowIfNull(installFlowController);
        return installFlowController.ExecuteAsync;
    }

    public async Task<InstallExecutionCoordinatorResult> RunAsync(
        InstallExecutionCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FlowRequest);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var operationOverlayMessage = ResolveInstallOperationOverlayMessage(
            request.SelectionStateBeforeExecution,
            request.Strings);
        request.ApplyInstallBusyState(true, null, operationOverlayMessage);
        var overlayTimer = Stopwatch.StartNew();
        InstallFlowResult? result = null;
        try
        {
            result = await _executeInstallFlowAsync(request.FlowRequest, cancellationToken);
            return new InstallExecutionCoordinatorResult
            {
                Result = result
            };
        }
        finally
        {
            try
            {
                await DelayForMinimumOverlayDurationAsync(
                    request,
                    overlayTimer.Elapsed,
                    result,
                    cancellationToken);
            }
            finally
            {
                request.ApplyInstallBusyState(false, request.SelectionStateBeforeExecution, "");
            }
        }
    }

    private static async Task DelayForMinimumOverlayDurationAsync(
        InstallExecutionCoordinatorRequest request,
        TimeSpan elapsed,
        InstallFlowResult? result,
        CancellationToken cancellationToken)
    {
        if (result is null || !result.DidStart || result.WasBlocked)
        {
            return;
        }

        var remaining = request.MinimumOperationOverlayDuration - elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        await request.DelayAsync(remaining, cancellationToken);
    }

    private static string ResolveInstallOperationOverlayMessage(
        ShellInstallSelectionState selectionState,
        AppStrings strings)
    {
        var statusCode = NormalizeStatusCode(selectionState.SelectedInstallStatusCode, InstallStatusCodes.Installable);
        if (string.Equals(statusCode, InstallStatusCodes.UpdateAvailable, StringComparison.OrdinalIgnoreCase))
        {
            return strings.OperationOverlayUpdating;
        }

        if (string.Equals(statusCode, InstallStatusCodes.Latest, StringComparison.OrdinalIgnoreCase))
        {
            return strings.OperationOverlayReinstalling;
        }

        return strings.OperationOverlayInstalling;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record InstallExecutionCoordinatorResult
{
    public InstallFlowResult Result { get; init; } = new();
}

public sealed record InstallExecutionCoordinatorRequest
{
    public required InstallFlowRequest FlowRequest { get; init; }
    public required ShellInstallSelectionState SelectionStateBeforeExecution { get; init; }
    public required AppStrings Strings { get; init; }
    public required Action<bool, ShellInstallSelectionState?, string> ApplyInstallBusyState { get; init; }
    public TimeSpan MinimumOperationOverlayDuration { get; init; } = TimeSpan.FromSeconds(1);
    public Func<TimeSpan, CancellationToken, Task> DelayAsync { get; init; } = Task.Delay;
}

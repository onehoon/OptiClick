using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.Install.Flow;

public sealed class InstallExecutionCoordinator
{
    private readonly InstallFlowController _installFlowController;

    public InstallExecutionCoordinator(InstallFlowController installFlowController)
    {
        _installFlowController = installFlowController ?? throw new ArgumentNullException(nameof(installFlowController));
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
        try
        {
            return new InstallExecutionCoordinatorResult
            {
                Result = await _installFlowController.ExecuteAsync(request.FlowRequest, cancellationToken)
            };
        }
        finally
        {
            request.ApplyInstallBusyState(false, request.SelectionStateBeforeExecution, "");
        }
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
}

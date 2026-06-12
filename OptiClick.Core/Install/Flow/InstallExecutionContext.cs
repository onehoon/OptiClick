using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Core.RuntimeData;
using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install.Flow;

public sealed record InstallExecutionContext
{
    public required InstallExecutionDescriptor ExecutionDescriptor { get; init; }
    public required AttachedRuntimeProfileRows ProfileRows { get; init; }
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } = new();
    public required bool IsEnabled { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public required InstallFlowSelectionSnapshot SelectionSnapshot { get; init; }
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
    public required bool IsWindowsSupported { get; init; }
    public required bool IsInstallExecutionInProgress { get; init; }
    public required bool IsAppUpdateInProgress { get; init; }
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}

public sealed record InstallFlowReadinessSnapshot
{
    public bool HasRemoteCatalogError { get; init; }
    public bool IsSheetLoading { get; init; }
    public bool IsSheetReady { get; init; } = true;

    public static InstallFlowReadinessSnapshot Create(InstallExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var selection = context.SelectionSnapshot;
        return Create(
            context.LatestRemoteCatalogErrorCode,
            selection.SheetLoading,
            selection.SheetReady);
    }

    public static InstallFlowReadinessSnapshot Create(
        string? latestRemoteCatalogErrorCode,
        bool isSheetLoading,
        bool isSheetReady)
    {
        var hasRemoteCatalogError = !string.IsNullOrWhiteSpace((latestRemoteCatalogErrorCode ?? "").Trim());
        return new InstallFlowReadinessSnapshot
        {
            HasRemoteCatalogError = hasRemoteCatalogError,
            IsSheetLoading = isSheetLoading,
            IsSheetReady = !hasRemoteCatalogError && isSheetReady
        };
    }
}

public sealed record InstallFlowSelectionGateSnapshot
{
    public bool IsMultiGpuBlocked { get; init; }
    public bool IsGpuSelectionPending { get; init; }
    public bool HasValidMatch { get; init; }
    public bool IsPopupConfirmed { get; init; }
    public bool HasPendingPopupRequests { get; init; }
    public string ActionAvailabilityReasonCode { get; init; } = "";

    public static InstallFlowSelectionGateSnapshot Create(InstallFlowSelectionSnapshot selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return new InstallFlowSelectionGateSnapshot
        {
            IsMultiGpuBlocked = selection.MultiGpuBlocked,
            IsGpuSelectionPending = selection.GpuSelectionPending,
            HasValidMatch = selection.HasValidMatch,
            IsPopupConfirmed = selection.PopupConfirmed,
            HasPendingPopupRequests = selection.HasPendingPopupRequests,
            ActionAvailabilityReasonCode = selection.ActionAvailabilityReasonCode
        };
    }
}

public sealed record InstallFlowOperationGateSnapshot
{
    public bool IsWindowsSupported { get; init; } = true;
    public bool IsInstallExecutionInProgress { get; init; }
    public bool IsAppUpdateInProgress { get; init; }
    public bool IsEnabled { get; init; } = true;

    public static InstallFlowOperationGateSnapshot Create(InstallExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InstallFlowOperationGateSnapshot
        {
            IsWindowsSupported = context.IsWindowsSupported,
            IsInstallExecutionInProgress = context.IsInstallExecutionInProgress,
            IsAppUpdateInProgress = context.IsAppUpdateInProgress,
            IsEnabled = context.IsEnabled
        };
    }
}

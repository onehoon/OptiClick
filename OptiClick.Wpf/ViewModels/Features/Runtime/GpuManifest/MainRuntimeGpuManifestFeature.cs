using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Runtime;

namespace OptiClick.Wpf.ViewModels.Features.Runtime.GpuManifest;

internal sealed class MainRuntimeGpuManifestFeature
{
    private readonly GpuSelectionCoordinator _gpuSelectionCoordinator;
    private readonly MainRuntimeCatalogUiFlowController _runtimeCatalogUiFlowController;
    private readonly MainRuntimeCatalogUiFlowContextFactory _runtimeCatalogUiFlowContextFactory;

    public MainRuntimeGpuManifestFeature(
        GpuSelectionCoordinator gpuSelectionCoordinator,
        MainRuntimeCatalogUiFlowController runtimeCatalogUiFlowController,
        MainRuntimeCatalogUiFlowContextFactory runtimeCatalogUiFlowContextFactory)
    {
        _gpuSelectionCoordinator =
            gpuSelectionCoordinator ?? throw new ArgumentNullException(nameof(gpuSelectionCoordinator));
        _runtimeCatalogUiFlowController =
            runtimeCatalogUiFlowController ?? throw new ArgumentNullException(nameof(runtimeCatalogUiFlowController));
        _runtimeCatalogUiFlowContextFactory =
            runtimeCatalogUiFlowContextFactory ?? throw new ArgumentNullException(nameof(runtimeCatalogUiFlowContextFactory));
    }

    public bool MultiGpuBlocked => _gpuSelectionCoordinator.MultiGpuBlocked;
    public bool GpuSelectionPending => _gpuSelectionCoordinator.GpuSelectionPending;
    public string SelectedGpuLogSource => _gpuSelectionCoordinator.SelectedGpuLogSource;
    public string RestartRequiredErrorCode => MainRuntimeCatalogUiFlowController.GpuManifestRestartRequiredErrorCode;

    public void ApplyMultiGpuBlockedUiState()
    {
        _runtimeCatalogUiFlowController.ApplyMultiGpuBlockedUiState(CreateCatalogUiFlowContext());
    }

    public void ApplyGpuManifestRestartRequiredState(string detailErrorCode)
    {
        _runtimeCatalogUiFlowController.ApplyGpuManifestRestartRequiredState(
            CreateCatalogUiFlowContext(),
            detailErrorCode);
    }

    public void ClearGpuManifestRestartRequiredState()
    {
        _runtimeCatalogUiFlowController.ClearGpuManifestRestartRequiredState(CreateCatalogUiFlowContext());
    }

    public Task ShowGpuManifestRestartRequiredDialogOnceAsync(
        string errorCode,
        CancellationToken cancellationToken)
    {
        return _runtimeCatalogUiFlowController.ShowGpuManifestRestartRequiredDialogOnceAsync(
            CreateCatalogUiFlowContext(),
            errorCode,
            cancellationToken);
    }

    public Task<IReadOnlyList<GpuInfo>> ResolveManifestSupportedGpuCandidatesAsync(
        RuntimeContext runtimeContext,
        IReadOnlyList<GpuInfo> detectedCandidates,
        CancellationToken cancellationToken)
    {
        return _runtimeCatalogUiFlowController.ResolveManifestSupportedGpuCandidatesAsync(
            CreateCatalogUiFlowContext(),
            runtimeContext,
            detectedCandidates,
            cancellationToken);
    }

    private MainRuntimeCatalogUiFlowContext CreateCatalogUiFlowContext()
    {
        return _runtimeCatalogUiFlowContextFactory.Create();
    }
}

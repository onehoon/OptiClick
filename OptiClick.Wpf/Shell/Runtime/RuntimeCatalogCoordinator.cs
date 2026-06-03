using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeCatalogCoordinator
{
    private readonly RuntimeCatalogFlowController _runtimeCatalogFlowController;
    private readonly RuntimeEndpointStatusPresenter _runtimeEndpointStatusPresenter;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public RuntimeCatalogCoordinator(
        RuntimeCatalogFlowController runtimeCatalogFlowController,
        RuntimeEndpointStatusPresenter runtimeEndpointStatusPresenter)
    {
        _runtimeCatalogFlowController = runtimeCatalogFlowController ?? throw new ArgumentNullException(nameof(runtimeCatalogFlowController));
        _runtimeEndpointStatusPresenter = runtimeEndpointStatusPresenter ?? throw new ArgumentNullException(nameof(runtimeEndpointStatusPresenter));
    }

    public async Task RefreshAsync(
        RuntimeCatalogCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasGameCardFactory)
        {
            return;
        }

        if (request.IsMultiGpuBlocked)
        {
            request.ApplyMultiGpuBlockedUiState();
            return;
        }

        if (request.IsGpuSelectionPending)
        {
            return;
        }

        if (request.IsGpuManifestRestartRequired)
        {
            var detailCode = NormalizeStatusCode(
                request.LatestRemoteCatalogDetailErrorCode,
                request.GpuManifestRestartRequiredErrorCode);
            request.ApplyGpuManifestRestartRequiredState(detailCode);
            await request.ShowGpuManifestRestartRequiredDialogOnceAsync(detailCode, cancellationToken);
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            request.ApplySettingsStatusText(
                _runtimeEndpointStatusPresenter.BuildStatus(
                    request.LatestRuntimeContext.RemoteData,
                    request.Strings));

            var result = await _runtimeCatalogFlowController.RefreshAsync(
                request.BuildRuntimeCatalogRequest(
                    request.LatestRuntimeContext,
                    request.SelectedLanguage,
                    request.Strings),
                cancellationToken);
            await request.ApplyRuntimeCatalogFlowResultAsync(
                result,
                request.RefreshMode,
                cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record RuntimeCatalogCoordinatorRequest
{
    public required bool HasGameCardFactory { get; init; }

    public required bool IsMultiGpuBlocked { get; init; }

    public required bool IsGpuSelectionPending { get; init; }

    public required bool IsGpuManifestRestartRequired { get; init; }

    public required string LatestRemoteCatalogDetailErrorCode { get; init; }

    public required string GpuManifestRestartRequiredErrorCode { get; init; }

    public required RuntimeContext LatestRuntimeContext { get; init; }

    public required AppLanguage SelectedLanguage { get; init; }

    public required AppStrings Strings { get; init; }

    public required RuntimeCatalogRefreshMode RefreshMode { get; init; }

    public required Func<RuntimeContext, AppLanguage, AppStrings, RuntimeCatalogFlowRequest> BuildRuntimeCatalogRequest { get; init; }

    public required Action ApplyMultiGpuBlockedUiState { get; init; }

    public required Action<string> ApplyGpuManifestRestartRequiredState { get; init; }

    public required Func<string, CancellationToken, Task> ShowGpuManifestRestartRequiredDialogOnceAsync { get; init; }

    public required Action<string> ApplySettingsStatusText { get; init; }

    public required Func<RuntimeCatalogFlowResult, RuntimeCatalogRefreshMode, CancellationToken, Task> ApplyRuntimeCatalogFlowResultAsync { get; init; }
}

using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.ViewModels.Features.Runtime;

namespace OptiClick.Wpf.ViewModels.Ports.Runtime;

internal sealed record MainShellRuntimePortCompositionInput
{
    public required IMainShellRuntimePortAccess Access { get; init; }
    public required Func<MainRuntimeFeatureFacade> ResolveRuntimeFeature { get; init; }
}

internal static class MainShellRuntimePortComposer
{
    public static MainShellFacadeRuntimePort Compose(MainShellRuntimePortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var access = input.Access;

        return new MainShellFacadeRuntimePort
        {
            RuntimeShellState = access.RuntimeShellState,
            ScannedGameState = access.ScannedGameState,
            RefreshRuntimeContextAsync =
                cancellationToken => input.ResolveRuntimeFeature().RefreshRuntimeContextAsync(cancellationToken),
            RefreshRuntimeDataCatalogForStartupAsync =
                cancellationToken => input.ResolveRuntimeFeature()
                    .RefreshRuntimeDataCatalogForStartupAsync(cancellationToken),
            RefreshDeviceIdentityRulesAsync =
                cancellationToken => input.ResolveRuntimeFeature().RefreshDeviceIdentityRulesAsync(cancellationToken),
            ApplyDeviceIdentityRulesFromCacheAsync =
                cancellationToken => input.ResolveRuntimeFeature().ApplyLocalDeviceIdentityRulesAsync(
                    RuntimeSummaryStateText.FromAppStrings(access.Strings),
                    access.ApplyRuntimeSummaryStateUpdate,
                    cancellationToken),
            StartDeviceIdentityRulesRefreshInBackground = access.StartDeviceIdentityRulesRefreshInBackground,
            BuildLatestRuntimeSummaryStateUpdate = () =>
                input.ResolveRuntimeFeature().BuildLatestRuntimeSummaryStateUpdate(
                    RuntimeSummaryStateText.FromAppStrings(access.Strings)),
            ApplyRuntimeSummaryStateUpdate = access.ApplyRuntimeSummaryStateUpdate,
            IsMultiGpuBlocked = () => input.ResolveRuntimeFeature().MultiGpuBlocked,
            ResolveManifestSupportedGpuCandidatesAsync =
                (runtimeContext, candidates, ct) =>
                    input.ResolveRuntimeFeature().ResolveManifestSupportedGpuCandidatesAsync(
                        runtimeContext,
                        candidates,
                        ct),
            ApplyMultiGpuBlockedUiState = () => input.ResolveRuntimeFeature().ApplyMultiGpuBlockedUiState(),
            RefreshRuntimeCatalogAsync = access.RefreshRuntimeDataCatalogByModeAsync,
            RefreshRuntimeCatalogWithSelectedGpuAsync =
                (selectedGpu, refreshMode, cancellationToken) =>
                    input.ResolveRuntimeFeature().RefreshRuntimeDataCatalogWithSelectedGpuAsync(
                        selectedGpu,
                        refreshMode,
                        cancellationToken)
        };
    }
}

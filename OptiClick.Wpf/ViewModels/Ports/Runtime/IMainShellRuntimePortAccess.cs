using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels.Ports.Runtime;

internal interface IMainShellRuntimePortAccess
{
    AppStrings Strings { get; }
    RuntimeShellState RuntimeShellState { get; }
    ScannedGameState ScannedGameState { get; }
    void StartDeviceIdentityRulesRefreshInBackground();
    void ApplyRuntimeSummaryStateUpdate(RuntimeSummaryStateUpdate update);
    Task RefreshRuntimeDataCatalogByModeAsync(
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken);
}

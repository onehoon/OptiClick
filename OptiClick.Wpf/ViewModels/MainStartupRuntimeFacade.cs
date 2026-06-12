using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Shell.Runtime;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainStartupRuntimeFacade
{
    private readonly MainStartupOrchestrator _startupOrchestrator;
    private readonly MainRuntimeOrchestrator _runtimeOrchestrator;

    public MainStartupRuntimeFacade(
        MainStartupOrchestrator startupOrchestrator,
        MainRuntimeOrchestrator runtimeOrchestrator)
    {
        _startupOrchestrator = startupOrchestrator;
        _runtimeOrchestrator = runtimeOrchestrator;
    }

    public Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(
        MainStartupOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        return _startupOrchestrator.ShowStartupOperatingSystemBlockIfNeededAsync(context, cancellationToken);
    }

    public Task InitializeAsync(
        MainStartupOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        return _startupOrchestrator.InitializeAsync(context, cancellationToken);
    }

    public Task RefreshRuntimeContextAsync(
        MainRuntimeContextRefreshContext context,
        CancellationToken cancellationToken = default)
    {
        return _runtimeOrchestrator.RefreshRuntimeContextAsync(context, cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogAsync(
        MainRuntimeCatalogRefreshContext context,
        RuntimeCatalogRefreshMode refreshMode,
        CancellationToken cancellationToken = default)
    {
        return _runtimeOrchestrator.RefreshRuntimeDataCatalogAsync(context, refreshMode, cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogForStartupAsync(
        MainRuntimeCatalogRefreshContext context,
        CancellationToken cancellationToken = default)
    {
        return _runtimeOrchestrator.RefreshRuntimeDataCatalogForStartupAsync(context, cancellationToken);
    }

    public Task RefreshDeviceIdentityRulesAsync(
        MainDeviceIdentityRulesContext context,
        CancellationToken cancellationToken = default)
    {
        return _runtimeOrchestrator.RefreshDeviceIdentityRulesAsync(context, cancellationToken);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainStartupOrchestrator
{
    public async Task<bool> ShowStartupOperatingSystemBlockIfNeededAsync(
        MainStartupOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.ShouldBlockStartupForUnsupportedOperatingSystem())
        {
            return false;
        }

        await context.ShowStartupBlockDialogAsync(cancellationToken);
        return true;
    }

    public async Task InitializeAsync(
        MainStartupOrchestratorContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.RunInitialStartupAsync(
                context.BuildStartupFlowRequest(),
                cancellationToken);
            context.UpdateStartupPreparationState(state => state with
            {
                LastErrorCode = context.ClearLastErrorCode(state.LastErrorCode, context.StartupInitializationErrorCode)
            });
        }
        catch (Exception ex)
        {
            context.LogStartupInitializationError(ex);
            context.UpdateStartupPreparationState(state => state with { LastErrorCode = context.StartupInitializationErrorCode });
            context.SetSettingsStatusText(context.StartupInitializationWarningText);
        }
        finally
        {
            await context.ShowPendingStartupNoticesAsync(cancellationToken);
        }
    }
}

internal sealed class MainStartupOrchestratorContext
{
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<CancellationToken, Task> ShowStartupBlockDialogAsync { get; init; }
    public required Func<StartupFlowRequest> BuildStartupFlowRequest { get; init; }
    public required Func<StartupFlowRequest, CancellationToken, Task> RunInitialStartupAsync { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
    public required Action<Exception> LogStartupInitializationError { get; init; }
    public required Action<string> SetSettingsStatusText { get; init; }
    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Func<CancellationToken, Task> ShowPendingStartupNoticesAsync { get; init; }
    public required string StartupInitializationErrorCode { get; init; }
    public required string StartupInitializationWarningText { get; init; }
}

internal sealed class MainRuntimeOrchestrator
{
    public Task RefreshRuntimeContextAsync(
        MainRuntimeContextRefreshContext context,
        CancellationToken cancellationToken = default)
    {
        return context.RefreshRuntimeContextAsync(context.BuildRuntimeContextCoordinatorRequest(), cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogAsync(
        MainRuntimeCatalogRefreshContext context,
        RuntimeCatalogRefreshMode mode,
        CancellationToken cancellationToken = default)
    {
        return context.RefreshRuntimeCatalogAsync(mode, cancellationToken);
    }

    public Task RefreshRuntimeDataCatalogForStartupAsync(
        MainRuntimeCatalogRefreshContext context,
        CancellationToken cancellationToken = default)
    {
        return context.RefreshRuntimeCatalogAsync(RuntimeCatalogRefreshMode.BackgroundWarmup, cancellationToken);
    }

    public Task RefreshDeviceIdentityRulesAsync(
        MainDeviceIdentityRulesContext context,
        CancellationToken cancellationToken = default)
    {
        return context.RefreshAsync(cancellationToken);
    }
}

internal sealed class MainRuntimeContextRefreshContext
{
    public required Func<RuntimeContextCoordinatorRequest> BuildRuntimeContextCoordinatorRequest { get; init; }
    public required Func<RuntimeContextCoordinatorRequest, CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
}

internal sealed class MainRuntimeCatalogRefreshContext
{
    public required Func<RuntimeCatalogRefreshMode, CancellationToken, Task> RefreshRuntimeCatalogAsync { get; init; }
}

internal sealed class MainDeviceIdentityRulesContext
{
    public required Func<CancellationToken, Task> RefreshAsync { get; init; }
}

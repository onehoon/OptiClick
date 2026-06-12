using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallArchiveReadinessController
{
    public Task<ArchiveReadinessFlowResult> RefreshAsync(
        MainInstallArchiveReadinessContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Services.ArchiveReadinessRefreshCoordinator.RunForegroundRefreshAsync(
            ct => RefreshWithoutCoordinatorAsync(context, ct),
            cancellationToken);
    }

    public async Task<ArchiveReadinessFlowResult> RefreshWithoutCoordinatorAsync(
        MainInstallArchiveReadinessContext context,
        CancellationToken cancellationToken)
    {
        var result = await context.Services.ArchiveReadinessFlowController.RefreshAsync(
            new ArchiveReadinessFlowRequest
            {
                ModuleDownloadLinks = context.State.ModuleDownloadLinks,
                OptiScalerVariantCatalog = context.State.LatestOptiScalerVariantCatalog,
                PreferredOptiScalerVariant = context.State.PreferredOptiScalerVariant,
                Fsr4VariantCatalog = context.State.LatestFsr4VariantCatalog
            },
            cancellationToken);

        context.Services.DispatchFlowLogs(result.Logs, MainViewModelLogCategories.Install);
        if (!result.DidRun)
        {
            return result;
        }

        context.Callbacks.SetArchiveReadiness(result.Readiness);
        ApplyOptiScalerVariantSyncResult(context, result.OptiScalerVariantSync);
        return result;
    }

    private static void ApplyOptiScalerVariantSyncResult(
        MainInstallArchiveReadinessContext context,
        OptiScalerVariantSyncResult? result)
    {
        context.Callbacks.ApplyOptiScalerVariantSyncToRuntimeState(result);
        context.Callbacks.ApplyOptiScalerVariantOptions();

        if (result?.ShouldPersistEffectiveVariant != true)
        {
            return;
        }

        var effectiveVariant = string.IsNullOrWhiteSpace(result.EffectiveVariant)
            ? OptiScalerVariantCatalogBuilder.StableVariant
            : result.EffectiveVariant;
        context.Callbacks.PersistEffectiveVariantPreference(effectiveVariant);
        context.Callbacks.SaveUserSettings();
    }
}

internal sealed class MainInstallArchiveReadinessContext
{
    public required MainInstallArchiveReadinessState State { get; init; }
    public required MainInstallArchiveReadinessServices Services { get; init; }
    public required MainInstallArchiveReadinessCallbacks Callbacks { get; init; }
}

internal sealed class MainInstallArchiveReadinessState
{
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
    public required OptiScalerVariantCatalog LatestOptiScalerVariantCatalog { get; init; }
    public required string PreferredOptiScalerVariant { get; init; }
    public required Fsr4VariantCatalog LatestFsr4VariantCatalog { get; init; }
}

internal sealed class MainInstallArchiveReadinessServices
{
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
}

internal sealed class MainInstallArchiveReadinessCallbacks
{
    public required Action<ArchiveReadinessSnapshot> SetArchiveReadiness { get; init; }
    public required Action<OptiScalerVariantSyncResult?> ApplyOptiScalerVariantSyncToRuntimeState { get; init; }
    public required Action ApplyOptiScalerVariantOptions { get; init; }
    public required Action<string> PersistEffectiveVariantPreference { get; init; }
    public required Action SaveUserSettings { get; init; }
}

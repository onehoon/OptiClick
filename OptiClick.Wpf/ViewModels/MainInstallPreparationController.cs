using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallPreparationController
{
    public async Task<MainInstallExecutionPreparation?> PrepareAsync(
        MainInstallPreparationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.State.IsInstallExecutionInProgress())
        {
            return null;
        }

        var selectedGame = context.State.ResolveSelectedGame();
        if (selectedGame is null)
        {
            return null;
        }

        var selectedIndex = context.State.ResolveSelectedIndex(selectedGame);
        if (selectedIndex < 0)
        {
            return null;
        }

        await context.Services.RefreshArchiveReadinessAsync(cancellationToken);
        selectedGame = context.State.ResolveSelectedGame();
        if (selectedGame is null)
        {
            return null;
        }

        selectedIndex = context.State.ResolveSelectedIndex(selectedGame);
        if (selectedIndex < 0)
        {
            return null;
        }

        // This pre-install refresh reuses the current selection without showing already-reviewed popups.
        // If future precheck logic can create new blocking warnings here, those must be shown to the user.
        await context.Services.RefreshSelectionForInstallAsync(selectedGame, cancellationToken, false, false);

        var selectionStateBeforeExecution = context.State.ReadSelectionState();
        var latestArchiveReadiness = context.State.ReadLatestArchiveReadiness();
        var optiScalerIniApplyContext = context.Services.CreateOptiScalerIniApplyContext();
        var request = context.Services.BuildInstallRequest(
            ShellGameCardMapper.Map(selectedGame),
            context.State.LatestRuntimeContext,
            latestArchiveReadiness,
            selectionStateBeforeExecution,
            context.State.ModuleDownloadLinks,
            optiScalerIniApplyContext,
            context.State.IsOperatingSystemSupported(),
            context.State.IsInstallExecutionInProgress(),
            context.State.IsAppUpdateInProgress(),
            context.State.Text,
            context.State.LatestRemoteCatalogErrorCode);

        return new MainInstallExecutionPreparation(request, selectionStateBeforeExecution);
    }
}

internal sealed class MainInstallPreparationContext
{
    public required MainInstallPreparationState State { get; init; }
    public required MainInstallPreparationServices Services { get; init; }
}

internal sealed class MainInstallPreparationState
{
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required ModuleDownloadLinkContext ModuleDownloadLinks { get; init; }
    public required Func<bool> IsOperatingSystemSupported { get; init; }
    public required InstallFlowText Text { get; init; }
    public required string LatestRemoteCatalogErrorCode { get; init; }
}

internal sealed class MainInstallPreparationServices
{
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RefreshSelectionForInstallAsync { get; init; }
    public required Func<OptiScalerIniApplyContext> CreateOptiScalerIniApplyContext { get; init; }
    public required Func<
        ShellGameCardModel,
        RuntimeContext,
        ArchiveReadinessSnapshot,
        ShellInstallSelectionState,
        ModuleDownloadLinkContext,
        OptiScalerIniApplyContext,
        bool,
        bool,
        bool,
        InstallFlowText,
        string,
        InstallFlowRequest> BuildInstallRequest { get; init; }
}

internal sealed record MainInstallExecutionPreparation(
    InstallFlowRequest Request,
    ShellInstallSelectionState SelectionStateBeforeExecution);

using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Install.Planning;

public sealed class InstallPlanInputBuilder
{
    public InstallPlanBuildInput Build(InstallPlanInputBuildContext context)
    {
        var selectedShellGame = ShellGameCardMapper.Map(context.SelectedGame);
        context.TargetPathByGameId.TryGetValue(selectedShellGame.GameId, out var targetPath);
        context.MatchByGameId.TryGetValue(selectedShellGame.GameId, out var matchResult);
        var normalizedTargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(targetPath);
        var fsr4Required = ShellGameCardMapper.ResolveFsr4Required(selectedShellGame);

        return new InstallPlanBuildInput
        {
            SelectedGame = selectedShellGame,
            MatchResult = matchResult,
            RuntimeContext = context.LatestRuntimeContext,
            ActionAvailability = context.SelectionState.ActionAvailability,
            ArchiveReadiness = context.LatestArchiveReadiness,
            Precheck = context.SelectionState.PrecheckSnapshot,
            TargetFolderHint = normalizedTargetPath,
            MatchedExeHint = matchResult?.MatchedExe ?? selectedShellGame.MatchExe,
            IsInstallInProgress = context.IsInstallExecutionInProgress,
            IsPredownloadInProgress = false,
            IsMultiGpuBlocked = context.SelectionState.MultiGpuBlocked,
            IsAppUpdateInProgress = context.IsAppUpdateInProgress,
            IsSelectionPopupConfirmed = context.SelectionState.PopupConfirmed,
            IsGpuSelectionPending = context.SelectionState.GpuSelectionPending,
            IsSheetLoading = false,
            IsSheetReady = true,
            IsFsr4Required = fsr4Required
        };
    }
}

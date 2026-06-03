using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Archives;

public sealed class ArchiveReadinessFlowController
{
    private readonly IArchivePreparationCoordinator? _archivePreparationCoordinator;

    public ArchiveReadinessFlowController(IArchivePreparationCoordinator? archivePreparationCoordinator)
    {
        _archivePreparationCoordinator = archivePreparationCoordinator;
    }

    public async Task<ArchiveReadinessFlowResult> RefreshAsync(
        ArchiveReadinessFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_archivePreparationCoordinator is null)
        {
            return new ArchiveReadinessFlowResult
            {
                DidRun = false,
                IsSuccess = false
            };
        }

        var logs = new List<InstallFlowLogEntry>
        {
            Info("archive", "refresh start")
        };

        try
        {
            var optiScalerSnapshot = await _archivePreparationCoordinator.PrepareOptiScalerAsync(
                request.ModuleDownloadLinks,
                cancellationToken);
            var startupSnapshot = await _archivePreparationCoordinator.PrepareStartupArchivesAsync(
                request.ModuleDownloadLinks,
                request.Fsr4Enabled,
                cancellationToken);
            var merged = ArchivePreparationSnapshotMerger.Merge(optiScalerSnapshot, startupSnapshot);
            var readiness = ArchivePreparationSnapshotMapper.ToInstallPlanSnapshot(merged);

            logs.Add(Info("archive", $"optiscaler state={readiness.OptiScalerState}"));
            logs.Add(Info("archive", $"optipatcher state={readiness.OptiPatcherState}"));
            logs.Add(Info("archive", $"fsr4 state={readiness.Fsr4State}"));
            logs.Add(Info("archive", $"unreal5 state={readiness.Unreal5State}"));
            logs.Add(Info("archive", "refresh completed"));

            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = true,
                Readiness = readiness,
                Logs = logs
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logs.Add(Info("archive", "refresh canceled"));
            throw;
        }
        catch (Exception ex)
        {
            logs.Add(Error("archive", "refresh failed", ex));
            return new ArchiveReadinessFlowResult
            {
                DidRun = true,
                IsSuccess = false,
                Readiness = ArchiveReadinessSnapshot.NotReady,
                Logs = logs
            };
        }
    }

    private static InstallFlowLogEntry Info(string category, string message)
    {
        return new InstallFlowLogEntry
        {
            Level = "info",
            Category = category,
            Message = message
        };
    }

    private static InstallFlowLogEntry Error(string category, string message, Exception exception)
    {
        return new InstallFlowLogEntry
        {
            Level = "error",
            Category = category,
            Message = message,
            Exception = exception
        };
    }
}

using OptiClick.Wpf.Install.Planning;
using OptiClick.Core.Install;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Install.Flow;

internal static class InstallPlanSnapshotMapper
{
    public static InstallGameMatchSnapshot? FromShellMatch(ShellGameMatchResult? match)
    {
        if (match is null)
        {
            return null;
        }

        return new InstallGameMatchSnapshot
        {
            State = match.Status switch
            {
                ShellGameMatchStatus.Matched => InstallGameMatchState.Matched,
                ShellGameMatchStatus.MultipleCandidates => InstallGameMatchState.MultipleCandidates,
                ShellGameMatchStatus.Disabled => InstallGameMatchState.Disabled,
                ShellGameMatchStatus.UnsupportedGpu => InstallGameMatchState.UnsupportedGpu,
                _ => InstallGameMatchState.None
            },
            MatchedExe = (match.MatchedExe ?? "").Trim(),
            FolderPath = (match.FolderPath ?? "").Trim(),
            ReasonCode = (match.ReasonCode ?? "").Trim(),
            IsUnsupportedGpu = match.Status == ShellGameMatchStatus.UnsupportedGpu
        };
    }

    public static InstallActionAvailabilitySnapshot FromShellActionAvailability(ShellGameActionAvailability? availability)
    {
        return new InstallActionAvailabilitySnapshot
        {
            ReasonCode = (availability?.ReasonCode ?? "").Trim()
        };
    }
}

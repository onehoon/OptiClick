using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Fallbacks;

internal sealed class UnavailableInstallStartGateResolver : IInstallStartGateResolver
{
    public InstallStartGateDecision Resolve(InstallStartGateInput input)
    {
        _ = input;
        return new InstallStartGateDecision
        {
            CanStart = false,
            ReasonCode = InstallEntryRejectionCodes.InstallExecutionUnavailable,
            Stage = "composition"
        };
    }
}

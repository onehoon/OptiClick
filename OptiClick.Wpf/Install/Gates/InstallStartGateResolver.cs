using OptiClick.Core.Install;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Gates;

public sealed class InstallStartGateResolver : IInstallStartGateResolver
{
    private readonly IWritePermissionProbe _writePermissionProbe;
    private readonly IInstallTargetPathValidator _targetPathValidator;
    private readonly CoreInstallStartGatePolicy _coreGatePolicy;

    public InstallStartGateResolver(
        IWritePermissionProbe writePermissionProbe,
        CoreInstallStartGatePolicy? coreGatePolicy = null,
        IInstallTargetPathValidator? targetPathValidator = null)
    {
        _writePermissionProbe = writePermissionProbe;
        _targetPathValidator = targetPathValidator ?? new InstallTargetPathValidator();
        _coreGatePolicy = coreGatePolicy ?? new CoreInstallStartGatePolicy();
    }

    public InstallStartGateDecision Resolve(InstallStartGateInput input)
    {
        if (input is null)
        {
            return Reject(InstallStartGateReasonCodes.InvalidInstallPlan, "input");
        }

        var targetPathValidation = _targetPathValidator.Validate(input.TargetPath);
        var coreDecision = _coreGatePolicy.Resolve(
            InstallStartGateCoreInputMapper.Map(input, targetPathValidation.IsValidTargetDirectory));
        if (!coreDecision.CanStart)
        {
            return Reject(coreDecision.ReasonCode, coreDecision.Stage);
        }

        if (input.RequireWritePermissionProbe)
        {
            var probeResult = _writePermissionProbe.Probe(targetPathValidation.NormalizedTargetDirectory);
            if (!probeResult.IsSuccess)
            {
                return Reject(InstallStartGateReasonCodes.WritePermissionDenied, "permission");
            }
        }

        return new InstallStartGateDecision
        {
            CanStart = true,
            ReasonCode = InstallStartGateReasonCodes.Ready,
            Stage = "ready",
            RequiresPopup = false,
            PopupRequest = new PopupPresentationRequest
            {
                Kind = PopupPresentationKind.None,
                ReasonCode = InstallStartGateReasonCodes.Ready
            }
        };
    }

    private static InstallStartGateDecision Reject(string reasonCode, string stage)
    {
        return new InstallStartGateDecision
        {
            CanStart = false,
            ReasonCode = reasonCode,
            Stage = stage,
            RequiresPopup = true,
            PopupRequest = new PopupPresentationRequest
            {
                Kind = PopupPresentationKind.Warning,
                ReasonCode = reasonCode
            }
        };
    }
}

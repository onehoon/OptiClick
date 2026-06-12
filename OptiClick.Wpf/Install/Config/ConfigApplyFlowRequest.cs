using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Config;

public sealed record ConfigApplyFlowRequest
{
    public required ConfigApplyApplicationRequest ApplicationRequest { get; init; }
    public required string ConfigApplyFailureMessage { get; init; }
    public required bool InstallSucceeded { get; init; }
}

using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install.Flow;

public sealed record UninstallFlowSelectionSnapshot
{
    public string FinalProxyDllName { get; init; } = "";
    public InstallPrecheckSnapshot Precheck { get; init; } = InstallPrecheckSnapshot.NotStarted;
    public IReadOnlyList<string> UalDetectedNames { get; init; } = Array.Empty<string>();
}

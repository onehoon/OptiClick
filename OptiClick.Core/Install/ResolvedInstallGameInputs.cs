using OptiClick.Core.OptiScaler;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Core.Install;

public sealed record ResolvedInstallGameInputs
{
    public static ResolvedInstallGameInputs Empty { get; } = new();

    public InstallExecutionDescriptor ExecutionDescriptor { get; init; } = InstallExecutionDescriptor.Empty;
    public AttachedRuntimeProfileRows ProfileRows { get; init; } = AttachedRuntimeProfileRows.Empty;
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } =
        new();
    public IReadOnlyList<RuntimeDataRawRow> EngineIniProfileRows { get; init; } = [];
    public bool IsEnabled { get; init; }
    public string GameId => ExecutionDescriptor.GameId;
    public string MatchExe => ExecutionDescriptor.MatchExe;
}

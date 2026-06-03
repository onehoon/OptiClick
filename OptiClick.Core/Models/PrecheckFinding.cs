using OptiClick.Core.Install.Precheck;

namespace OptiClick.Core.Models;

public sealed record PrecheckFinding
{
    public PrecheckFindingType Type { get; init; } = PrecheckFindingType.Unknown;
    public string RelativePath { get; init; } = "";
    public DllOwnerKind OwnerKind { get; init; } = DllOwnerKind.Unknown;
    public string Warning { get; init; } = "";
}

namespace OptiClick.Infrastructure.Install.Gates;

public sealed record WritePermissionProbeResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
}


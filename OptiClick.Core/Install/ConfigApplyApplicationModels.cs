namespace OptiClick.Core.Install;

public sealed record ConfigApplyFlowOutcome
{
    public bool HasFailure { get; init; }
    public int TotalErrorCount { get; init; }
}

public static class ConfigApplyEventActions
{
    public const string Applied = "applied";
    public const string Skipped = "skipped";
    public const string StageError = "stage_error";
}

public sealed record ConfigApplyEvent
{
    public string ProfileName { get; init; } = "";
    public string Action { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public string TargetPath { get; init; } = "";
    public string TargetKey { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public string Detail { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
}

public sealed record ConfigApplyIssue
{
    public string ProfileName { get; init; } = "";
    public string ReasonCode { get; init; } = "";
    public string Detail { get; init; } = "";
    public string TargetPath { get; init; } = "";
    public string TargetKey { get; init; } = "";
    public string ValuePath { get; init; } = "";
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
}

public sealed record ConfigApplyApplicationResult
{
    public bool IsSuccess { get; init; } = true;
    public string FailureCode { get; init; } = "";
    public int ErrorCount { get; init; }
    public ConfigApplyFlowOutcome Outcome { get; init; } = new();
    public IReadOnlyList<ConfigApplyEvent> Events { get; init; } = [];
    public IReadOnlyList<ConfigApplyIssue> Issues { get; init; } = [];
    public Exception? Exception { get; init; }
}

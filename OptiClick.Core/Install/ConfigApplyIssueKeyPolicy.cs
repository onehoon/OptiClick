namespace OptiClick.Core.Install;

public static class ConfigApplyIssueKeyPolicy
{
    public static string Build(ConfigApplyIssue issue)
    {
        return string.Join(
            "\u001F",
            issue.ProfileName ?? "",
            issue.ReasonCode ?? "",
            issue.TargetPath ?? "",
            issue.TargetKey ?? "",
            issue.ValuePath ?? "",
            issue.Detail ?? "",
            issue.OldValue ?? "",
            issue.NewValue ?? "");
    }
}

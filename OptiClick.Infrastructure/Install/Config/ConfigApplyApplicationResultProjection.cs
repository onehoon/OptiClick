using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

internal static class ConfigApplyApplicationResultProjection
{
    public static IReadOnlyList<ConfigApplyEvent> BuildEvents(
        IEnumerable<ConfigProfileApplySummary> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        ArgumentNullException.ThrowIfNull(optiScalerIniSummaries);
        ArgumentNullException.ThrowIfNull(profileResult);

        var events = new List<ConfigApplyEvent>();
        foreach (var summary in EnumerateSummaries(optiScalerIniSummaries, profileResult))
        {
            foreach (var applied in summary.Applied)
            {
                events.Add(new ConfigApplyEvent
                {
                    ProfileName = summary.ProfileName,
                    Action = ConfigApplyEventActions.Applied,
                    TargetPath = applied.TargetPath,
                    TargetKey = applied.TargetKey,
                    ValuePath = applied.ValuePath,
                    OldValue = applied.OldValue,
                    NewValue = applied.NewValue
                });
            }

            foreach (var skipped in summary.Skipped)
            {
                events.Add(new ConfigApplyEvent
                {
                    ProfileName = summary.ProfileName,
                    Action = ConfigApplyEventActions.Skipped,
                    ReasonCode = skipped.ReasonCode,
                    TargetPath = skipped.TargetPath,
                    TargetKey = skipped.TargetKey,
                    ValuePath = skipped.ValuePath,
                    Detail = skipped.Detail,
                    OldValue = skipped.OldValue,
                    NewValue = skipped.NewValue
                });
            }

            if (!summary.Completed)
            {
                events.Add(new ConfigApplyEvent
                {
                    ProfileName = summary.ProfileName,
                    Action = ConfigApplyEventActions.StageError,
                    ReasonCode = ConfigErrorReasons.ApplyFailed,
                    TargetPath = summary.TargetPathHint
                });
            }
        }

        return events;
    }

    public static IReadOnlyList<ConfigApplyIssue> BuildIssues(
        IEnumerable<ConfigProfileApplySummary> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        ArgumentNullException.ThrowIfNull(optiScalerIniSummaries);
        ArgumentNullException.ThrowIfNull(profileResult);

        var issues = new List<ConfigApplyIssue>();
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var summary in EnumerateSummaries(optiScalerIniSummaries, profileResult))
        {
            foreach (var error in summary.Errors)
            {
                AddIssue(issues, knownKeys, error);
            }
        }

        foreach (var error in profileResult.Errors)
        {
            AddIssue(issues, knownKeys, error);
        }

        return issues;
    }

    private static IEnumerable<ConfigProfileApplySummary> EnumerateSummaries(
        IEnumerable<ConfigProfileApplySummary> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        foreach (var summary in optiScalerIniSummaries)
        {
            if (summary is not null)
            {
                yield return summary;
            }
        }

        foreach (var summary in profileResult.Summaries)
        {
            if (summary is not null)
            {
                yield return summary;
            }
        }
    }

    private static void AddIssue(
        ICollection<ConfigApplyIssue> issues,
        ISet<string> knownKeys,
        ConfigProfileError error)
    {
        var issue = new ConfigApplyIssue
        {
            ProfileName = error.ProfileName,
            ReasonCode = error.ReasonCode,
            Detail = error.Detail,
            TargetPath = error.TargetPath,
            TargetKey = error.TargetKey,
            ValuePath = error.ValuePath,
            OldValue = error.OldValue,
            NewValue = error.NewValue
        };

        if (!knownKeys.Add(ConfigApplyIssueKeyPolicy.Build(issue)))
        {
            return;
        }

        issues.Add(issue);
    }
}

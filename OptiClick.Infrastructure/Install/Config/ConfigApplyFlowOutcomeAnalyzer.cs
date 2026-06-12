using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

internal static class ConfigApplyFlowOutcomeAnalyzer
{
    public static ConfigApplyFlowOutcome Analyze(
        IEnumerable<ConfigProfileApplySummary?> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        ArgumentNullException.ThrowIfNull(optiScalerIniSummaries);
        ArgumentNullException.ThrowIfNull(profileResult);

        var iniSummaries = optiScalerIniSummaries
            .Where(static summary => summary is not null)
            .Select(static summary => summary!)
            .ToArray();
        var hasIniFailure = iniSummaries.Any(HasSummaryFailure);
        var hasProfileFailure = profileResult.Errors.Count > 0
                                || profileResult.Summaries.Any(HasSummaryFailure);

        return new ConfigApplyFlowOutcome
        {
            HasFailure = hasIniFailure || hasProfileFailure,
            TotalErrorCount = CountTotalErrors(iniSummaries, profileResult)
        };
    }

    private static int CountErrors(ConfigProfileApplySummary summary)
    {
        return Math.Max(summary.ErrorCount, summary.Errors.Count);
    }

    private static bool HasSummaryFailure(ConfigProfileApplySummary summary)
    {
        return !summary.Completed
               || summary.Errors.Count > 0
               || CountErrors(summary) > 0;
    }

    private static int CountTotalErrors(
        IEnumerable<ConfigProfileApplySummary> optiScalerIniSummaries,
        ConfigProfileApplyResult profileResult)
    {
        var total = optiScalerIniSummaries.Sum(CountErrors);
        var profileErrorKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var summary in profileResult.Summaries)
        {
            foreach (var error in summary.Errors)
            {
                profileErrorKeys.Add(ConfigProfileErrorKeyBuilder.Build(error));
            }

            var unlistedErrors = Math.Max(0, summary.ErrorCount - summary.Errors.Count);
            total += unlistedErrors;
        }

        foreach (var error in profileResult.Errors)
        {
            profileErrorKeys.Add(ConfigProfileErrorKeyBuilder.Build(error));
        }

        total += profileErrorKeys.Count;
        return total;
    }
}

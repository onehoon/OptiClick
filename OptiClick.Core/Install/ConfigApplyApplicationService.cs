using OptiClick.Core.OptiScaler;

namespace OptiClick.Core.Install;

public sealed class ConfigApplyApplicationService
{
    private readonly IConfigApplyProfileStageRunner? _profileStageRunner;
    private readonly IConfigApplyOptiScalerIniStageRunner? _optiScalerIniStageRunner;

    public ConfigApplyApplicationService(
        IConfigApplyProfileStageRunner? profileStageRunner,
        IConfigApplyOptiScalerIniStageRunner? optiScalerIniStageRunner)
    {
        _profileStageRunner = profileStageRunner;
        _optiScalerIniStageRunner = optiScalerIniStageRunner;
    }

    public ConfigApplyApplicationResult Apply(ConfigApplyApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var optiScalerIniApplyPlan = OptiScalerIniApplyPlanBuilder.Build(request.OptiScalerIniApplyContext);
        var hasProfileRows = request.ProfileRows.HasAnyRows;
        if (hasProfileRows && _profileStageRunner is null)
        {
            return CreateMissingDependencyFailure(ConfigApplyFailureCodes.ConfigProfileApplierMissing);
        }

        if (optiScalerIniApplyPlan.HasSettings && _optiScalerIniStageRunner is null)
        {
            return CreateMissingDependencyFailure(ConfigApplyFailureCodes.IniProfileEditorMissing);
        }

        var events = new List<ConfigApplyEvent>();
        var issues = new List<ConfigApplyIssue>();
        var knownIssueKeys = new HashSet<string>(StringComparer.Ordinal);
        var hasFailure = false;
        var totalErrorCount = 0;
        try
        {
            if (optiScalerIniApplyPlan.HasSettings)
            {
                var stageResult = _optiScalerIniStageRunner
                                  ?? throw new InvalidOperationException("OptiScaler INI stage runner must be available when INI settings are present.");
                MergeStageResult(stageResult.Apply(request.TargetFolder, optiScalerIniApplyPlan), events, issues, knownIssueKeys, ref hasFailure, ref totalErrorCount);
            }

            if (hasProfileRows)
            {
                var stageResult = _profileStageRunner
                                  ?? throw new InvalidOperationException("Config profile stage runner must be available when profile rows are present.");
                MergeStageResult(stageResult.Apply(request.TargetFolder, request.ProfileRows), events, issues, knownIssueKeys, ref hasFailure, ref totalErrorCount);
            }

            var outcome = new ConfigApplyFlowOutcome
            {
                HasFailure = hasFailure,
                TotalErrorCount = totalErrorCount
            };
            return outcome.HasFailure
                ? new ConfigApplyApplicationResult
                {
                    IsSuccess = false,
                    FailureCode = ConfigApplyFailureCodes.ConfigApplyFailed,
                    ErrorCount = Math.Max(outcome.TotalErrorCount, 1),
                    Outcome = outcome,
                    Events = events,
                    Issues = issues
                }
                : new ConfigApplyApplicationResult
                {
                    IsSuccess = true,
                    ErrorCount = outcome.TotalErrorCount,
                    Outcome = outcome,
                    Events = events,
                    Issues = issues
                };
        }
        catch (Exception ex)
        {
            return new ConfigApplyApplicationResult
            {
                IsSuccess = false,
                FailureCode = ConfigApplyFailureCodes.ConfigApplyException,
                ErrorCount = 1,
                Outcome = new ConfigApplyFlowOutcome
                {
                    HasFailure = true,
                    TotalErrorCount = 1
                },
                Events = events,
                Issues = issues,
                Exception = ex
            };
        }
    }

    private static ConfigApplyApplicationResult CreateMissingDependencyFailure(string failureCode)
    {
        return new ConfigApplyApplicationResult
        {
            IsSuccess = false,
            FailureCode = failureCode,
            ErrorCount = 1,
            Outcome = new ConfigApplyFlowOutcome
            {
                HasFailure = true,
                TotalErrorCount = 1
            }
        };
    }

    private static void MergeStageResult(
        ConfigApplyStageResult stageResult,
        ICollection<ConfigApplyEvent> events,
        ICollection<ConfigApplyIssue> issues,
        ISet<string> knownIssueKeys,
        ref bool hasFailure,
        ref int totalErrorCount)
    {
        ArgumentNullException.ThrowIfNull(stageResult);

        hasFailure |= stageResult.HasFailure;
        totalErrorCount += Math.Max(0, stageResult.ErrorCount);
        foreach (var configApplyEvent in stageResult.Events)
        {
            if (configApplyEvent is not null)
            {
                events.Add(configApplyEvent);
            }
        }

        foreach (var issue in stageResult.Issues)
        {
            if (issue is null)
            {
                continue;
            }

            if (!knownIssueKeys.Add(ConfigApplyIssueKeyPolicy.Build(issue)))
            {
                continue;
            }

            issues.Add(issue);
        }
    }

}

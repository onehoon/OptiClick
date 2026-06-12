using OptiClick.Core.Install;
using OptiClick.Core.OptiScaler;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class ConfigApplyOptiScalerIniStageRunner : IConfigApplyOptiScalerIniStageRunner
{
    private const string OptiScalerIniFileName = "OptiScaler.ini";
    private const string BaseSettingsProfileName = "optiscaler_ini_base";
    private const string CommonSettingsProfileName = "optiscaler_ini_common";

    private readonly IOptiScalerIniBaseApplier _optiScalerIniBaseApplier;

    public ConfigApplyOptiScalerIniStageRunner(IOptiScalerIniBaseApplier optiScalerIniBaseApplier)
    {
        _optiScalerIniBaseApplier = optiScalerIniBaseApplier ?? throw new ArgumentNullException(nameof(optiScalerIniBaseApplier));
    }

    public ConfigApplyStageResult Apply(string targetFolder, OptiScalerIniApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var summaries = Execute(targetFolder, plan);
        var profileResult = new ConfigProfileApplyResult();
        var outcome = ConfigApplyFlowOutcomeAnalyzer.Analyze(summaries, profileResult);
        return new ConfigApplyStageResult
        {
            HasFailure = outcome.HasFailure,
            ErrorCount = outcome.TotalErrorCount,
            Events = ConfigApplyApplicationResultProjection.BuildEvents(summaries, profileResult),
            Issues = ConfigApplyApplicationResultProjection.BuildIssues(summaries, profileResult)
        };
    }

    private IReadOnlyList<ConfigProfileApplySummary> Execute(string targetFolder, OptiScalerIniApplyPlan plan)
    {
        var summaries = new List<ConfigProfileApplySummary>();
        if (plan.GameSettings.Count > 0)
        {
            summaries.Add(_optiScalerIniBaseApplier.ApplyBase(
                targetFolder,
                OptiScalerIniFileName,
                plan.GameSettings,
                BaseSettingsProfileName));
        }

        if (plan.CommonSettings.Count > 0)
        {
            summaries.Add(_optiScalerIniBaseApplier.ApplyBase(
                targetFolder,
                OptiScalerIniFileName,
                plan.CommonSettings,
                CommonSettingsProfileName));
        }

        return summaries;
    }
}

public sealed class ConfigApplyProfileStageRunner : IConfigApplyProfileStageRunner
{
    private readonly IConfigProfileApplier _configProfileApplier;

    public ConfigApplyProfileStageRunner(IConfigProfileApplier configProfileApplier)
    {
        _configProfileApplier = configProfileApplier ?? throw new ArgumentNullException(nameof(configProfileApplier));
    }

    public ConfigApplyStageResult Apply(string targetPath, ConfigApplyProfileRows profileRows)
    {
        var profileResult = ConfigProfileApplyRunner.Apply(_configProfileApplier, targetPath, profileRows);
        var outcome = ConfigApplyFlowOutcomeAnalyzer.Analyze(Array.Empty<ConfigProfileApplySummary>(), profileResult);
        return new ConfigApplyStageResult
        {
            HasFailure = outcome.HasFailure,
            ErrorCount = outcome.TotalErrorCount,
            Events = ConfigApplyApplicationResultProjection.BuildEvents(Array.Empty<ConfigProfileApplySummary>(), profileResult),
            Issues = ConfigApplyApplicationResultProjection.BuildIssues(Array.Empty<ConfigProfileApplySummary>(), profileResult)
        };
    }
}

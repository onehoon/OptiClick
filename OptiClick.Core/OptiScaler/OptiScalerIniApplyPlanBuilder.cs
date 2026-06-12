namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerIniApplyPlan
{
    public IReadOnlyDictionary<string, string> GameSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> CommonSettings { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool HasSettings => GameSettings.Count > 0 || CommonSettings.Count > 0;
}

public static class OptiScalerIniApplyPlanBuilder
{
    public static OptiScalerIniApplyPlan Build(OptiScalerIniApplyContext? context)
    {
        var resolvedContext = context ?? new OptiScalerIniApplyContext();
        return new OptiScalerIniApplyPlan
        {
            GameSettings = resolvedContext.NormalizeGameOptiScalerIniSettings(),
            CommonSettings = resolvedContext.NormalizeCommonOptiScalerIniSettings()
        };
    }
}

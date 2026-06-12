using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallPlanSummaryPolicy
{
    public IReadOnlyList<CoreInstallPlanWarning> ResolveWarnings(IReadOnlyList<InstallPrecheckFinding> findings)
    {
        if (findings is null || findings.Count == 0)
        {
            return Array.Empty<CoreInstallPlanWarning>();
        }

        var warnings = new List<CoreInstallPlanWarning>(findings.Count);
        foreach (var finding in findings)
        {
            var code = (finding.Kind ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            warnings.Add(new CoreInstallPlanWarning
            {
                Code = code,
                Detail = (finding.Message ?? "").Trim(),
                IsBlockingCandidate = finding.IsBlocking
            });
        }

        return warnings;
    }

    public CoreInstallPlanSummary BuildSummary(
        string finalProxyDllName,
        IReadOnlyList<CoreInstallPlanComponent> components,
        IReadOnlyList<CoreInstallPlanWarning> warnings)
    {
        return new CoreInstallPlanSummary
        {
            OptiScalerTargetDll = finalProxyDllName,
            SelectedComponents = components
                .Where(static component => component.Enabled)
                .Select(static component => component.Type.ToString())
                .ToArray(),
            WarningCodes = warnings.Select(static warning => warning.Code).ToArray(),
            Notes =
            [
                "Dry-run plan only. No filesystem/network side-effects are executed."
            ]
        };
    }
}

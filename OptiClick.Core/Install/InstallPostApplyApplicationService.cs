using OptiClick.Core.Install.Components;
using OptiClick.Core.OptiScaler;

namespace OptiClick.Core.Install;

public sealed class InstallPostApplyApplicationService
{
    private readonly ConfigApplyApplicationService _configApplyApplicationService;
    private readonly IInstallRtssProfileApplier _rtssProfileApplier;

    public InstallPostApplyApplicationService(
        ConfigApplyApplicationService configApplyApplicationService,
        IInstallRtssProfileApplier rtssProfileApplier)
    {
        _configApplyApplicationService = configApplyApplicationService
                                         ?? throw new ArgumentNullException(nameof(configApplyApplicationService));
        _rtssProfileApplier = rtssProfileApplier
                              ?? throw new ArgumentNullException(nameof(rtssProfileApplier));
    }

    public InstallPostApplyResult Execute(InstallPostApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);
        ArgumentNullException.ThrowIfNull(request.InstallResult);

        var coreSucceeded = IsCoreInstallSuccessful(request.InstallResult);
        if (!coreSucceeded)
        {
            return new InstallPostApplyResult
            {
                CoreInstallSucceeded = false,
                ConfigApplySkipped = true
            };
        }

        var configApplyResult = _configApplyApplicationService.Apply(new ConfigApplyApplicationRequest
        {
            TargetFolder = request.Plan.TargetFolder,
            ProfileRows = request.Plan.ProfileRows,
            OptiScalerIniApplyContext = request.OptiScalerIniApplyContext
        });
        var rtssApplyResult = _rtssProfileApplier.Apply(new InstallRtssProfileApplyRequest
        {
            RequiresRtssProfile = ShouldApplyRtssProfile(request.Plan),
            MatchExe = request.Plan.MatchExe
        });

        return new InstallPostApplyResult
        {
            CoreInstallSucceeded = true,
            ConfigApplyResult = configApplyResult,
            RtssLogs = rtssApplyResult.Logs
        };
    }

    private static bool IsCoreInstallSuccessful(InstallPostApplyInstallResultSnapshot installResult)
    {
        var coreStep = installResult.Steps.FirstOrDefault(static step =>
            step.Component == ComponentInstallName.OptiScalerCore);
        if (coreStep is not null)
        {
            return coreStep.Status == ComponentInstallStatus.Success;
        }

        return installResult.IsSuccess;
    }

    private static bool ShouldApplyRtssProfile(InstallPostApplyPlanSnapshot plan)
    {
        return plan.Components.Any(static component =>
            component.Enabled
            && component.Type == InstallPostApplyPlanComponentType.RtssProfile);
    }
}

public sealed record InstallPostApplyRequest
{
    public required InstallPostApplyPlanSnapshot Plan { get; init; }
    public required InstallPostApplyInstallResultSnapshot InstallResult { get; init; }
    public OptiScalerIniApplyContext OptiScalerIniApplyContext { get; init; } = new();
}

public sealed record InstallPostApplyResult
{
    public bool CoreInstallSucceeded { get; init; }
    public bool ConfigApplySkipped { get; init; }
    public ConfigApplyApplicationResult ConfigApplyResult { get; init; } = new();
    public IReadOnlyList<InstallRtssProfileApplyLogEntry> RtssLogs { get; init; } = [];
}

public sealed record InstallPostApplyPlanSnapshot
{
    public required string TargetFolder { get; init; }
    public required string MatchExe { get; init; }
    public ConfigApplyProfileRows ProfileRows { get; init; } = ConfigApplyProfileRows.Empty;
    public IReadOnlyList<InstallPostApplyPlanComponentSnapshot> Components { get; init; } = [];
}

public sealed record InstallPostApplyPlanComponentSnapshot
{
    public InstallPostApplyPlanComponentType Type { get; init; }
    public bool Enabled { get; init; }
}

public enum InstallPostApplyPlanComponentType
{
    Unknown = 0,
    RtssProfile
}

public sealed record InstallPostApplyInstallResultSnapshot
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<ComponentInstallStepResult> Steps { get; init; } = [];
}

public interface IInstallRtssProfileApplier
{
    InstallRtssProfileApplyResult Apply(InstallRtssProfileApplyRequest request);
}

public sealed record InstallRtssProfileApplyRequest
{
    public bool RequiresRtssProfile { get; init; }
    public string MatchExe { get; init; } = "";
}

public sealed record InstallRtssProfileApplyResult
{
    public IReadOnlyList<InstallRtssProfileApplyLogEntry> Logs { get; init; } = [];
}

public sealed record InstallRtssProfileApplyLogEntry
{
    public string Level { get; init; } = "info";
    public string Category { get; init; } = "config";
    public string Message { get; init; } = "";
}

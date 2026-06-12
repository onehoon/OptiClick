using OptiClick.Core.Install;
using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Config;
using CoreInstallPostApplyRequest = OptiClick.Core.Install.InstallPostApplyRequest;
using CoreInstallPostApplyResult = OptiClick.Core.Install.InstallPostApplyResult;

namespace OptiClick.Wpf.Install.Flow;

internal sealed class InstallPostApplyExecutor
{
    private readonly InstallPostApplyApplicationService _applicationService;
    private readonly ConfigApplyInstallLogAdapter _logAdapter;

    public InstallPostApplyExecutor(
        InstallPostApplyApplicationService applicationService,
        ConfigApplyInstallLogAdapter logAdapter)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _logAdapter = logAdapter ?? throw new ArgumentNullException(nameof(logAdapter));
    }

    public InstallPostApplyResult Execute(InstallPostApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var logs = new List<InstallFlowLogEntry>();
        var applicationResult = _applicationService.Execute(CreateApplicationRequest(request));
        var configApplyLogs = CreateConfigApplyLogs(applicationResult, request.ConfigApplyRequest.TargetFolder);
        var configApplyResult = CreateConfigApplyFlowResult(
            applicationResult,
            request.ConfigApplyFailureMessage,
            configApplyLogs);

        logs.AddRange(configApplyLogs);
        logs.AddRange(applicationResult.RtssLogs.Select(ToInstallFlowLogEntry));

        return new InstallPostApplyResult
        {
            ConfigApplyResult = configApplyResult,
            Logs = logs
        };
    }

    private static CoreInstallPostApplyRequest CreateApplicationRequest(InstallPostApplyRequest request)
    {
        return new CoreInstallPostApplyRequest
        {
            Plan = new InstallPostApplyPlanSnapshot
            {
                TargetFolder = request.ConfigApplyRequest.TargetFolder,
                MatchExe = request.Plan.MatchedExe,
                ProfileRows = request.ConfigApplyRequest.ProfileRows,
                Components = request.Plan.Components.Select(ToCoreComponentSnapshot).ToArray()
            },
            InstallResult = new InstallPostApplyInstallResultSnapshot
            {
                IsSuccess = request.InstallResult.IsSuccess,
                Steps = request.InstallResult.Steps
            },
            OptiScalerIniApplyContext = request.ConfigApplyRequest.OptiScalerIniApplyContext
        };
    }

    private IReadOnlyList<InstallFlowLogEntry> CreateConfigApplyLogs(
        CoreInstallPostApplyResult applicationResult,
        string targetPath)
    {
        return applicationResult.ConfigApplySkipped
            ? []
            : _logAdapter.Convert(applicationResult.ConfigApplyResult, targetPath);
    }

    private static ConfigApplyFlowResult CreateConfigApplyFlowResult(
        CoreInstallPostApplyResult applicationResult,
        string configApplyFailureMessage,
        IReadOnlyList<InstallFlowLogEntry> logs)
    {
        if (applicationResult.ConfigApplySkipped)
        {
            return ConfigApplyFlowResultFactory.Skipped();
        }

        var configApplyResult = applicationResult.ConfigApplyResult;
        return configApplyResult.IsSuccess
            ? ConfigApplyFlowResultFactory.Success(configApplyResult.ErrorCount, logs)
            : ConfigApplyFlowResultFactory.Failure(
                configApplyFailureMessage,
                configApplyResult.FailureCode,
                logs,
                configApplyResult.ErrorCount);
    }

    private static InstallPostApplyPlanComponentSnapshot ToCoreComponentSnapshot(CoreInstallPlanComponent component)
    {
        return new InstallPostApplyPlanComponentSnapshot
        {
            Enabled = component.Enabled,
            Type = component.Type == CoreInstallPlanComponentType.RtssProfile
                ? InstallPostApplyPlanComponentType.RtssProfile
                : InstallPostApplyPlanComponentType.Unknown
        };
    }

    private static InstallFlowLogEntry ToInstallFlowLogEntry(InstallRtssProfileApplyLogEntry entry)
    {
        return new InstallFlowLogEntry
        {
            Level = string.IsNullOrWhiteSpace(entry.Level) ? "info" : entry.Level,
            Category = string.IsNullOrWhiteSpace(entry.Category) ? "config" : entry.Category,
            Message = entry.Message ?? ""
        };
    }
}

internal sealed record InstallPostApplyRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public required ConfigApplyApplicationRequest ConfigApplyRequest { get; init; }
    public required string ConfigApplyFailureMessage { get; init; }
}

internal sealed record InstallPostApplyResult
{
    public required ConfigApplyFlowResult ConfigApplyResult { get; init; }
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = [];
}

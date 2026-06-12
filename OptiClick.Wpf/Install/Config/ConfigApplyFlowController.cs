using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Config;

public sealed class ConfigApplyFlowController
{
    private readonly ConfigApplyApplicationService _applicationService;
    private readonly ConfigApplyInstallLogAdapter _logAdapter;

    public ConfigApplyFlowController(
        ConfigApplyApplicationService applicationService,
        ConfigApplyInstallLogAdapter logAdapter)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _logAdapter = logAdapter ?? throw new ArgumentNullException(nameof(logAdapter));
    }

    public ConfigApplyFlowResult Apply(ConfigApplyFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ConfigApplyFailureMessage))
        {
            throw new ArgumentException("Config apply failure message is required.", nameof(request.ConfigApplyFailureMessage));
        }

        var configApplyFailureMessage = request.ConfigApplyFailureMessage;
        if (!request.InstallSucceeded)
        {
            return ConfigApplyFlowResultFactory.Skipped();
        }

        var applicationResult = _applicationService.Apply(request.ApplicationRequest);
        var logs = _logAdapter.Convert(applicationResult, request.ApplicationRequest.TargetFolder);
        if (!applicationResult.IsSuccess)
        {
            return ConfigApplyFlowResultFactory.Failure(
                configApplyFailureMessage,
                applicationResult.FailureCode,
                logs,
                applicationResult.ErrorCount);
        }

        return ConfigApplyFlowResultFactory.Success(applicationResult.ErrorCount, logs);
    }
}

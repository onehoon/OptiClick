using System.Globalization;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Flow;

public interface IInstallResultApplier
{
    InstallResultApplyResult Apply(InstallResultApplyRequest request);
}

public sealed class InstallResultApplier : IInstallResultApplier
{
    private readonly ConfigApplyFlowController _configApplyFlowController;
    private readonly IRtssProfileApplier _rtssProfileApplier;
    private readonly IInstallResultPresentationResolver? _installResultPresentationResolver;
    private readonly InstallCompletionMessageBuilder _installCompletionMessageBuilder;

    public InstallResultApplier(
        ConfigApplyFlowController configApplyFlowController,
        IRtssProfileApplier rtssProfileApplier,
        IInstallResultPresentationResolver? installResultPresentationResolver,
        InstallCompletionMessageBuilder? installCompletionMessageBuilder = null)
    {
        _configApplyFlowController = configApplyFlowController
                                     ?? throw new ArgumentNullException(nameof(configApplyFlowController));
        _rtssProfileApplier = rtssProfileApplier
                              ?? throw new ArgumentNullException(nameof(rtssProfileApplier));
        _installResultPresentationResolver = installResultPresentationResolver;
        _installCompletionMessageBuilder = installCompletionMessageBuilder ?? new InstallCompletionMessageBuilder();
    }

    public InstallResultApplyResult Apply(InstallResultApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Strings);

        var coreSucceeded = IsCoreInstallSuccessful(request.InstallResult);
        var logs = new List<InstallFlowLogEntry>();
        var configApplyResult = _configApplyFlowController.Apply(new ConfigApplyFlowRequest
        {
            Plan = request.Plan,
            OptiScalerIniSettings = request.SelectedGame.InstallMetadata?.IniSettings
                                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            CommonOptiScalerIniSettings = request.CommonOptiScalerIniSettings,
            Strings = request.Strings,
            InstallSucceeded = coreSucceeded
        });
        logs.AddRange(configApplyResult.Logs);

        if (coreSucceeded)
        {
            var rtssApplyResult = _rtssProfileApplier.Apply(request.SelectedGame, request.SelectionState.SelectedMatchResult);
            logs.AddRange(rtssApplyResult.Logs);
        }

        var finalSuccess = request.InstallResult.IsSuccess && configApplyResult.IsSuccess;
        var statusText = finalSuccess
            ? _installCompletionMessageBuilder.BuildInstallCompletionMessage(
                request.Plan.FinalProxyDllName,
                request.Strings.InstallCompleted,
                request.Strings.InstallCompletedWithName)
            : !string.IsNullOrWhiteSpace(configApplyResult.FailureMessage)
                ? configApplyResult.FailureMessage
                : Format(request.Strings.InstallFailed, request.InstallResult.FailedStep?.ErrorCode ?? "unknown_error");
        var popupCompletionMessage = finalSuccess
            ? _installCompletionMessageBuilder.BuildInstallPostCompletionMessage(
                request.Plan.FinalProxyDllName,
                request.Strings.InstallPostCompletedWithNameTemplate)
            : "";

        var presentation = _installResultPresentationResolver?.Resolve(new InstallResultPresentationInput
        {
            Success = finalSuccess,
            Message = statusText
        });

        PopupPresentationRequest? popupRequest = null;
        if (presentation?.PopupRequest is { Kind: not PopupPresentationKind.None } candidatePopup)
        {
            popupRequest = finalSuccess
                ? candidatePopup with
                {
                    TitleKey = request.Strings.InstallCompleteDialogTitle,
                    BodyKey = "",
                    BodyDetail = _installCompletionMessageBuilder.BuildAfterInstallPopupMessage(
                        string.IsNullOrWhiteSpace(popupCompletionMessage) ? statusText : popupCompletionMessage,
                        request.SelectionState.InstallPostPopupMessage)
                }
                : candidatePopup;
        }

        return new InstallResultApplyResult
        {
            FinalSuccess = finalSuccess,
            StatusText = statusText,
            ConfigErrorCount = configApplyResult.ErrorCount,
            ConfigFailureCode = configApplyResult.FailureCode,
            PopupRequest = popupRequest,
            Logs = logs
        };
    }

    private static bool IsCoreInstallSuccessful(ComponentInstallResult installResult)
    {
        if (installResult is null)
        {
            return false;
        }

        var coreStep = installResult.Steps.FirstOrDefault(static step =>
            step.Component == ComponentInstallName.OptiScalerCore);
        if (coreStep is not null)
        {
            return coreStep.Status == ComponentInstallStatus.Success;
        }

        return installResult.IsSuccess;
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }

}

using OptiClick.Core.Install.Planning;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Install.Flow;

internal sealed class InstallResultPresentationFactory
{
    private readonly IInstallResultPresentationResolver? _installResultPresentationResolver;
    private readonly InstallCompletionMessageBuilder _installCompletionMessageBuilder;

    public InstallResultPresentationFactory(
        IInstallResultPresentationResolver? installResultPresentationResolver,
        InstallCompletionMessageBuilder? installCompletionMessageBuilder = null)
    {
        _installResultPresentationResolver = installResultPresentationResolver;
        _installCompletionMessageBuilder = installCompletionMessageBuilder ?? new InstallCompletionMessageBuilder();
    }

    public InstallResultPresentationFactoryResult Create(InstallResultPresentationFactoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var statusText = request.FinalSuccess
            ? _installCompletionMessageBuilder.BuildInstallCompletionMessage(
                request.Plan.FinalProxyDllName,
                request.Text.InstallCompleted,
                request.Text.InstallCompletedWithName)
            : !string.IsNullOrWhiteSpace(request.ConfigApplyResult.FailureMessage)
                ? request.ConfigApplyResult.FailureMessage
                : LocalizedTextFormatter.Format(
                    request.Text.InstallFailed,
                    request.InstallResult.FailedStep?.ErrorCode ?? "unknown_error");
        var popupCompletionMessage = request.FinalSuccess
            ? _installCompletionMessageBuilder.BuildInstallPostCompletionMessage(
                request.Plan.FinalProxyDllName,
                request.Text.InstallPostCompletedWithNameTemplate)
            : "";

        var presentation = _installResultPresentationResolver?.Resolve(new InstallResultPresentationInput
        {
            Success = request.FinalSuccess,
            Message = statusText
        });

        PopupPresentationRequest? popupRequest = null;
        if (presentation?.PopupRequest is { Kind: not PopupPresentationKind.None } candidatePopup)
        {
            popupRequest = request.FinalSuccess
                ? candidatePopup with
                {
                    TitleKey = request.Text.InstallCompleteDialogTitle,
                    BodyKey = "",
                    BodyDetail = _installCompletionMessageBuilder.BuildAfterInstallPopupMessage(
                        string.IsNullOrWhiteSpace(popupCompletionMessage) ? statusText : popupCompletionMessage,
                        request.InstallPostPopupMessage)
                }
                : candidatePopup;
        }

        return new InstallResultPresentationFactoryResult
        {
            StatusText = statusText,
            PopupRequest = popupRequest
        };
    }

}

internal sealed record InstallResultPresentationFactoryRequest
{
    public required CoreInstallPlan Plan { get; init; }
    public required ComponentInstallResult InstallResult { get; init; }
    public required ConfigApplyFlowResult ConfigApplyResult { get; init; }
    public required InstallFlowText Text { get; init; }
    public string InstallPostPopupMessage { get; init; } = "";
    public required bool FinalSuccess { get; init; }
}

internal sealed record InstallResultPresentationFactoryResult
{
    public required string StatusText { get; init; }
    public PopupPresentationRequest? PopupRequest { get; init; }
}

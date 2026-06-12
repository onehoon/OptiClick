using OptiClick.Core.Install;
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
    private readonly InstallPostApplyExecutor _postApplyExecutor;
    private readonly InstallResultPresentationFactory _presentationFactory;

    public InstallResultApplier(
        ConfigApplyApplicationService configApplyApplicationService,
        IInstallRtssProfileApplier rtssProfileApplier,
        IInstallResultPresentationResolver? installResultPresentationResolver,
        InstallCompletionMessageBuilder? installCompletionMessageBuilder = null)
        : this(
            new InstallPostApplyExecutor(
                new InstallPostApplyApplicationService(configApplyApplicationService, rtssProfileApplier),
                new ConfigApplyInstallLogAdapter()),
            new InstallResultPresentationFactory(
                installResultPresentationResolver,
                installCompletionMessageBuilder))
    {
    }

    internal InstallResultApplier(
        InstallPostApplyExecutor postApplyExecutor,
        InstallResultPresentationFactory presentationFactory)
    {
        _postApplyExecutor = postApplyExecutor ?? throw new ArgumentNullException(nameof(postApplyExecutor));
        _presentationFactory = presentationFactory ?? throw new ArgumentNullException(nameof(presentationFactory));
    }

    public InstallResultApplyResult Apply(InstallResultApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Text);

        var postApplyResult = _postApplyExecutor.Execute(new InstallPostApplyRequest
        {
            Plan = request.Plan,
            InstallResult = request.InstallResult,
            ConfigApplyRequest = new ConfigApplyApplicationRequest
            {
                TargetFolder = request.Plan.TargetFolder,
                ProfileRows = ConfigApplyProfileRowsMapper.FromAttachedRuntimeProfileRows(request.ProfileRows),
                OptiScalerIniApplyContext = request.OptiScalerIniApplyContext
            },
            ConfigApplyFailureMessage = request.Text.InstallFailedConfigApply
        });

        var finalSuccess = request.InstallResult.IsSuccess && postApplyResult.ConfigApplyResult.IsSuccess;
        var presentation = _presentationFactory.Create(new InstallResultPresentationFactoryRequest
        {
            Plan = request.Plan,
            InstallResult = request.InstallResult,
            ConfigApplyResult = postApplyResult.ConfigApplyResult,
            Text = request.Text,
            InstallPostPopupMessage = request.InstallPostPopupMessage,
            FinalSuccess = finalSuccess
        });

        return new InstallResultApplyResult
        {
            FinalSuccess = finalSuccess,
            StatusText = presentation.StatusText,
            ConfigErrorCount = postApplyResult.ConfigApplyResult.ErrorCount,
            ConfigFailureCode = postApplyResult.ConfigApplyResult.FailureCode,
            PopupRequest = presentation.PopupRequest,
            Logs = postApplyResult.Logs
        };
    }
}

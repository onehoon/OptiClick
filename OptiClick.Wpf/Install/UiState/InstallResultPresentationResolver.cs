namespace OptiClick.Wpf.Install.UiState;

public interface IInstallResultPresentationResolver
{
    InstallResultPresentation Resolve(InstallResultPresentationInput input);
}

public sealed class InstallResultPresentationResolver : IInstallResultPresentationResolver
{
    public InstallResultPresentation Resolve(InstallResultPresentationInput input)
    {
        if (input.Success)
        {
            return new InstallResultPresentation
            {
                ClearInstallInProgress = true,
                ShouldUpdateButtonState = true,
                ShouldUpdateCardInstallStatus = true,
                ShouldUpdateInstallSummary = true,
                PopupRequest = new PopupPresentationRequest
                {
                    Kind = PopupPresentationKind.Info,
                    TitleKey = "dialogs.after_install_title",
                    BodyKey = "dialogs.after_install_body"
                }
            };
        }

        return new InstallResultPresentation
        {
            ClearInstallInProgress = true,
            ShouldUpdateButtonState = true,
            ShouldUpdateCardInstallStatus = false,
            ShouldUpdateInstallSummary = false,
            PopupRequest = new PopupPresentationRequest
            {
                Kind = PopupPresentationKind.Error,
                TitleKey = "common.error",
                BodyKey = "dialogs.install_failed_body_template",
                BodyDetail = (input.Message ?? "").Trim()
            }
        };
    }
}

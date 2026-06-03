using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Install.Presentation;

public sealed class InstallPopupPresenter
{
    public PopupPresentationRequest ResolveInstallRejection(
        InstallStartGateDecision decision,
        IInstallRejectionPresentationResolver? installRejectionPresentationResolver)
    {
        var rejectionDecision = new InstallEntryGateDecision
        {
            Ok = false,
            Code = decision.ReasonCode,
            Detail = decision.Stage
        };
        return installRejectionPresentationResolver?.Resolve(rejectionDecision)
               ?? new PopupPresentationRequest
               {
                   Kind = PopupPresentationKind.Warning,
                   ReasonCode = decision.ReasonCode,
                   BodyKey = decision.ReasonCode
               };
    }

    public AppDialogRequest BuildDialogRequest(PopupPresentationRequest popup, AppStrings strings)
    {
        return new AppDialogRequest
        {
            Kind = popup.Kind switch
            {
                PopupPresentationKind.Error => AppDialogKind.Blocking,
                PopupPresentationKind.Warning => AppDialogKind.Warning,
                _ => AppDialogKind.Info
            },
            Severity = popup.Kind switch
            {
                PopupPresentationKind.Error => DialogSeverity.Blocking,
                PopupPresentationKind.Warning => DialogSeverity.Warning,
                _ => DialogSeverity.Info
            },
            Title = string.IsNullOrWhiteSpace(popup.TitleKey) ? strings.InstallDialogTitle : popup.TitleKey,
            Summary = string.IsNullOrWhiteSpace(popup.BodyDetail) ? popup.BodyKey : popup.BodyDetail
        };
    }
}

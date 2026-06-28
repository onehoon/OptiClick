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
        var safeStrings = strings ?? new AppStrings();
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
            Title = ResolveDialogText(
                popup.TitleKey,
                safeStrings,
                string.IsNullOrWhiteSpace(safeStrings.InstallDialogTitle) ? "Install" : safeStrings.InstallDialogTitle),
            Summary = ResolveDialogDetail(
                string.IsNullOrWhiteSpace(popup.BodyDetail) ? popup.BodyKey : popup.BodyDetail,
                safeStrings)
        };
    }

    private static string ResolveDialogDetail(string value, AppStrings strings)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        if (text.Contains("dialogs.", StringComparison.OrdinalIgnoreCase)
            || text.Contains("common.", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(
                "\n\n",
                text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.None)
                    .Select(part => ResolveDialogText(part, strings, part)));
        }

        return text;
    }

    private static string ResolveDialogText(string key, AppStrings strings, string fallback)
    {
        var normalized = (key ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        var korean = IsKorean(strings);
        return normalized switch
        {
            "common.warning" => korean ? "경고" : "Warning",
            "common.error" => korean ? "오류" : "Error",
            "common.notice" => string.IsNullOrWhiteSpace(strings.InstallNoticeDialogTitle)
                ? korean ? "안내" : "Notice"
                : strings.InstallNoticeDialogTitle,
            "dialogs.invalid_game_body" => korean
                ? "선택한 게임 정보를 다시 확인할 수 없습니다. 게임을 다시 선택한 뒤 설치를 다시 시도해 주세요."
                : "The selected game could not be verified. Select the game again, then try the installation again.",
            "dialogs.select_game_card_body" => string.IsNullOrWhiteSpace(strings.HomeSelectGameHint)
                ? normalized
                : strings.HomeSelectGameHint,
            "dialogs.preparing_download_title" => korean ? "다운로드 준비 중" : "Preparing download",
            "dialogs.preparing_download_body" => korean
                ? "필요한 설치 파일을 준비하고 있습니다. 잠시 후 다시 시도해 주세요."
                : "Required installation files are being prepared. Please try again shortly.",
            "dialogs.preparing_archive_title" => korean ? "설치 파일 준비 중" : "Preparing install files",
            "dialogs.preparing_archive_body" => korean
                ? "OptiScaler 설치 파일을 준비하고 있습니다. 잠시 후 다시 시도해 주세요."
                : "OptiScaler install files are being prepared. Please try again shortly.",
            "dialogs.precheck_incomplete_body" => korean
                ? "설치 전 확인이 아직 완료되지 않았습니다."
                : "The pre-install check has not completed yet.",
            "dialogs.precheck_retry_mods_body" => korean
                ? "게임을 다시 선택하거나 실행 중인 게임/모드를 종료한 뒤 다시 시도해 주세요."
                : "Select the game again, or close the running game/mods and try again.",
            "dialogs.optiscaler_archive_not_ready" => korean
                ? "OptiScaler 설치 파일이 아직 준비되지 않았습니다."
                : "OptiScaler install files are not ready yet.",
            "dialogs.confirm_popup_body" => korean
                ? "설치 전 안내를 확인한 뒤 다시 시도해 주세요."
                : "Confirm the installation notice, then try again.",
            "dialogs.unsupported_os_body" => string.IsNullOrWhiteSpace(strings.UnsupportedOperatingSystemSummary)
                ? normalized
                : strings.UnsupportedOperatingSystemSummary,
            "dialogs.install_permission_denied_body" => korean
                ? "설치 권한이 부족합니다. 관리자 권한으로 다시 실행한 뒤 시도해 주세요."
                : "Installation permission was denied. Relaunch as administrator, then try again.",
            "dialogs.install_rejected" => korean
                ? "현재 상태에서는 설치를 시작할 수 없습니다."
                : "The installation cannot start in the current state.",
            "dialogs.install_failed_body_template" => string.IsNullOrWhiteSpace(strings.InstallFailed)
                ? normalized
                : strings.InstallFailed,
            "dialogs.after_install_title" => string.IsNullOrWhiteSpace(strings.InstallCompleteDialogTitle)
                ? normalized
                : strings.InstallCompleteDialogTitle,
            "dialogs.after_install_body" => string.IsNullOrWhiteSpace(strings.InstallPostCompletedWithNameTemplate)
                ? normalized
                : strings.InstallPostCompletedWithNameTemplate,
            _ => normalized
        };
    }

    private static bool IsKorean(AppStrings strings)
    {
        return string.Equals(strings.DialogButtonOk, "확인", StringComparison.Ordinal);
    }
}

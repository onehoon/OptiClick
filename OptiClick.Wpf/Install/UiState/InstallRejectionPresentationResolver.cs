namespace OptiClick.Wpf.Install.UiState;

public interface IInstallRejectionPresentationResolver
{
    PopupPresentationRequest Resolve(InstallEntryGateDecision decision);
    PopupPresentationRequest ResolvePermissionDenied();
}

public sealed class InstallRejectionPresentationResolver : IInstallRejectionPresentationResolver
{
    public PopupPresentationRequest Resolve(InstallEntryGateDecision decision)
    {
        if (decision.Ok)
        {
            return None("");
        }

        var reasonCode = (decision.Code ?? "").Trim();
        return reasonCode switch
        {
            InstallEntryRejectionCodes.MultiGpuBlocked => None(reasonCode),
            InstallEntryRejectionCodes.InstallPrecheckRunning => None(reasonCode),
            InstallEntryRejectionCodes.InstallInProgress => None(reasonCode),
            InstallEntryRejectionCodes.InstallExecutionUnavailable => None(reasonCode),

            InstallEntryRejectionCodes.PredownloadInProgress => Info(reasonCode, "dialogs.preparing_download_title", "dialogs.preparing_download_body"),
            InstallEntryRejectionCodes.NoGameSelected => Warning(reasonCode, "common.warning", "dialogs.select_game_card_body"),
            InstallEntryRejectionCodes.OptiScalerArchiveDownloading => Info(reasonCode, "dialogs.preparing_archive_title", "dialogs.preparing_archive_body"),

            InstallEntryRejectionCodes.PrecheckIncomplete => Warning(
                reasonCode,
                "common.warning",
                "dialogs.precheck_incomplete_body",
                string.IsNullOrWhiteSpace(decision.Detail)
                    ? "dialogs.precheck_retry_mods_body"
                    : $"{decision.Detail}\n\ndialogs.precheck_retry_mods_body"),

            InstallEntryRejectionCodes.OptiScalerArchiveNotReady => Warning(
                reasonCode,
                "common.warning",
                "dialogs.optiscaler_archive_not_ready",
                decision.Detail),

            InstallEntryRejectionCodes.InvalidGameSelection => Warning(reasonCode, "common.warning", "dialogs.invalid_game_body"),

            InstallEntryRejectionCodes.ConfirmPopupRequired => Warning(reasonCode, "common.notice", "dialogs.confirm_popup_body"),
            InstallEntryRejectionCodes.UnsupportedOs => Warning(reasonCode, "common.warning", "dialogs.unsupported_os_body"),
            InstallEntryRejectionCodes.InvalidMatch => Warning(reasonCode, "common.warning", "dialogs.invalid_game_body"),
            InstallEntryRejectionCodes.InvalidTargetFolder => Warning(reasonCode, "common.warning", "dialogs.invalid_game_body"),
            InstallEntryRejectionCodes.FinalProxyMissing => Warning(reasonCode, "common.warning", "dialogs.install_rejected"),
            InstallEntryRejectionCodes.ProxyChainUnresolved => Warning(reasonCode, "common.warning", "dialogs.install_rejected"),
            InstallEntryRejectionCodes.InvalidInstallPlan => Warning(reasonCode, "common.warning", "dialogs.install_rejected"),
            InstallEntryRejectionCodes.WritePermissionDenied => Warning(
                reasonCode,
                "common.warning",
                "dialogs.install_permission_denied_body"),
            _ => Warning(reasonCode, "common.warning", "dialogs.install_rejected")
        };
    }

    public PopupPresentationRequest ResolvePermissionDenied()
    {
        return new PopupPresentationRequest
        {
            Kind = PopupPresentationKind.Error,
            TitleKey = "common.error",
            BodyKey = "dialogs.install_permission_denied_body",
            ReasonCode = InstallEntryRejectionCodes.WritePermissionDenied
        };
    }

    private static PopupPresentationRequest None(string reasonCode) => new()
    {
        Kind = PopupPresentationKind.None,
        ReasonCode = reasonCode
    };

    private static PopupPresentationRequest Info(string reasonCode, string titleKey, string bodyKey) => new()
    {
        Kind = PopupPresentationKind.Info,
        TitleKey = titleKey,
        BodyKey = bodyKey,
        ReasonCode = reasonCode
    };

    private static PopupPresentationRequest Warning(string reasonCode, string titleKey, string bodyKey, string bodyDetail = "") => new()
    {
        Kind = PopupPresentationKind.Warning,
        TitleKey = titleKey,
        BodyKey = bodyKey,
        BodyDetail = (bodyDetail ?? "").Trim(),
        ReasonCode = reasonCode
    };
}

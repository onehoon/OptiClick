namespace OptiClick.Wpf.Install.Precheck;

public sealed class InstallSelectionPrecheckFlow
{
    public InstallSelectionPrecheckFlowResult Build(
        int selectedIndex,
        int foundGameCount,
        InstallSelectionPrecheckOutcome? outcome,
        string selectionPopupMessage)
    {
        if (selectedIndex < 0 || selectedIndex >= foundGameCount || outcome is null)
        {
            return new InstallSelectionPrecheckFlowResult
            {
                State = new InstallSelectionUiState
                {
                    PopupConfirmed = false,
                    PrecheckRunning = false,
                    PrecheckOk = false
                }
            };
        }

        var state = new InstallSelectionUiState
        {
            PopupConfirmed = false,
            PrecheckRunning = false,
            PrecheckOk = outcome.Ok,
            PrecheckError = outcome.Error,
            PrecheckDllName = outcome.ResolvedDllName
        };

        var modNoticeMessage = (outcome.ModNoticeMessage ?? "").Trim();
        var modNoticeBulletItems = NormalizeBulletItems(outcome.ModNoticeBulletItems);
        var modNoticeFooterText = (outcome.ModNoticeFooterText ?? "").Trim();
        var hasModNotice = !string.IsNullOrWhiteSpace(modNoticeMessage)
                           || modNoticeBulletItems.Count > 0
                           || !string.IsNullOrWhiteSpace(modNoticeFooterText);
        var popupMessage = (outcome.PopupMessage ?? "").Trim();
        var precheckPopupMessage = string.Join("\n\n", new[] { popupMessage, modNoticeMessage }.Where(message => !string.IsNullOrWhiteSpace(message))).Trim();
        if (!outcome.Ok)
        {
            if (!string.IsNullOrWhiteSpace(precheckPopupMessage) || modNoticeBulletItems.Count > 0 || !string.IsNullOrWhiteSpace(modNoticeFooterText))
            {
                return new InstallSelectionPrecheckFlowResult
                {
                    State = state,
                    PopupRequests = new[]
                    {
                        new PopupRequest
                        {
                            Type = PopupRequestType.Precheck,
                            Message = precheckPopupMessage,
                            BulletItems = modNoticeBulletItems,
                            FooterText = modNoticeFooterText
                        }
                    }
                };
            }

            return new InstallSelectionPrecheckFlowResult
            {
                State = state
            };
        }

        var normalizedSelectionPopupMessage = (selectionPopupMessage ?? "").Trim();
        var requests = new List<PopupRequest>();
        if (hasModNotice)
        {
            requests.Add(new PopupRequest
            {
                Type = PopupRequestType.ModNotice,
                Message = modNoticeMessage,
                BulletItems = modNoticeBulletItems,
                FooterText = modNoticeFooterText
            });
        }

        if (!string.IsNullOrWhiteSpace(normalizedSelectionPopupMessage))
        {
            requests.Add(new PopupRequest
            {
                Type = PopupRequestType.Selection,
                Message = normalizedSelectionPopupMessage
            });
        }

        if (requests.Count == 0)
        {
            return new InstallSelectionPrecheckFlowResult
            {
                State = state with { PopupConfirmed = true },
                ConfirmedImmediately = true,
                ConfirmAfterPopupChain = false
            };
        }

        return new InstallSelectionPrecheckFlowResult
        {
            State = state,
            PopupRequests = requests,
            ConfirmAfterPopupChain = true
        };
    }

    private static IReadOnlyList<string> NormalizeBulletItems(IEnumerable<string>? source)
    {
        return (source ?? Array.Empty<string>())
            .Select(static item => (item ?? "").Trim())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }
}

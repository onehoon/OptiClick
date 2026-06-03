namespace OptiClick.Wpf.Install.UiState;

public interface IInstallUiTextProvider
{
    string InstallButton { get; }
    string InstallingButton { get; }
    string UpdateButton { get; }
    string UpdatingButton { get; }
    string ReinstallButton { get; }
    string ReinstallingButton { get; }
    string LoadingButton { get; }
}

public sealed record DefaultInstallUiTextProvider : IInstallUiTextProvider
{
    public string InstallButton { get; init; } = "Install";
    public string InstallingButton { get; init; } = "Installing";
    public string UpdateButton { get; init; } = "Update";
    public string UpdatingButton { get; init; } = "Updating";
    public string ReinstallButton { get; init; } = "Reinstall";
    public string ReinstallingButton { get; init; } = "Reinstalling";
    public string LoadingButton { get; init; } = "Loading";
}

public interface IInstallButtonPresentationResolver
{
    InstallButtonPresentation Resolve(
        InstallButtonState state,
        string selectedInstallStatusCode,
        IInstallUiTextProvider? textProvider = null);
}

public sealed class InstallButtonPresentationResolver : IInstallButtonPresentationResolver
{
    public InstallButtonPresentation Resolve(
        InstallButtonState state,
        string selectedInstallStatusCode,
        IInstallUiTextProvider? textProvider = null)
    {
        var strings = textProvider ?? new DefaultInstallUiTextProvider();
        var statusCode = (selectedInstallStatusCode ?? "").Trim().ToLowerInvariant();

        string text;
        if (state.ShowInstalling)
        {
            text = ResolveInstallText(strings, statusCode, showInstalling: true);
        }
        else if (state.Enabled)
        {
            text = ResolveInstallText(strings, statusCode, showInstalling: false);
        }
        else if (string.Equals(state.ReasonCode, InstallButtonReasonCodes.SheetLoading, StringComparison.OrdinalIgnoreCase))
        {
            text = strings.LoadingButton;
        }
        else
        {
            text = "";
        }

        return new InstallButtonPresentation
        {
            IsEnabled = state.Enabled,
            ShowInstalling = state.ShowInstalling,
            IsLoadingBlinkReason = string.Equals(state.ReasonCode, InstallButtonReasonCodes.SheetLoading, StringComparison.OrdinalIgnoreCase),
            ReasonCode = state.ReasonCode,
            Text = text
        };
    }

    private static string ResolveInstallText(IInstallUiTextProvider strings, string statusCode, bool showInstalling)
    {
        if (string.Equals(statusCode, InstallStatusCodes.UpdateAvailable, StringComparison.OrdinalIgnoreCase))
        {
            return showInstalling
                ? Pick(strings.UpdatingButton, strings.InstallingButton)
                : Pick(strings.UpdateButton, strings.InstallButton);
        }

        if (string.Equals(statusCode, InstallStatusCodes.Latest, StringComparison.OrdinalIgnoreCase))
        {
            return showInstalling
                ? Pick(strings.ReinstallingButton, strings.InstallingButton)
                : Pick(strings.ReinstallButton, strings.InstallButton);
        }

        return showInstalling
            ? Pick(strings.InstallingButton, strings.InstallingButton)
            : Pick(strings.InstallButton, strings.InstallButton);
    }

    private static string Pick(string value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return (fallback ?? "").Trim();
    }
}

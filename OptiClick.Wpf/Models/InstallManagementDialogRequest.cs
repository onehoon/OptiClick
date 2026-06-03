namespace OptiClick.Wpf.Models;

public sealed record InstallManagementDialogRequest
{
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string DestructiveText { get; init; } = "";
    public string PrimaryText { get; init; } = "";
    public string CancelText { get; init; } = "";
}

public enum InstallManagementDialogResult
{
    Cancel,
    ContinueInstall,
    Uninstall
}

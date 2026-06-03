namespace OptiClick.Wpf.Models;

public sealed record AppDialogRequest
{
    public AppDialogKind Kind { get; init; } = AppDialogKind.Info;
    public DialogSeverity Severity { get; init; } = DialogSeverity.Info;
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public IReadOnlyList<string> BulletItems { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AppDialogSection> Sections { get; init; } = Array.Empty<AppDialogSection>();
    public string PrimaryButtonText { get; init; } = "";
    public string SecondaryButtonText { get; init; } = "";
    public DialogButtonRole PrimaryButtonRole { get; init; } = DialogButtonRole.Default;
    public DialogButtonRole SecondaryButtonRole { get; init; } = DialogButtonRole.Default;
    public AppDialogResult PrimaryResult { get; init; } = AppDialogResult.None;
    public AppDialogResult SecondaryResult { get; init; } = AppDialogResult.None;
    public bool RequiresAcknowledgement { get; init; }
    public string AcknowledgementText { get; init; } = "";
    public bool IsBlocking { get; init; }
    public string ErrorCode { get; init; } = "";
    public string FooterText { get; init; } = "";
    public bool CanClose { get; init; } = true;
    public bool CloseOnOverlayClick { get; init; }
    public string IconGlyphOverride { get; init; } = "";
}

public sealed record AppDialogSection
{
    public AppDialogSectionKind Kind { get; init; } = AppDialogSectionKind.Normal;
    public string Title { get; init; } = "";
    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
}

public enum AppDialogSectionKind
{
    Normal,
    NewGameSupport,
    StartupWarning
}

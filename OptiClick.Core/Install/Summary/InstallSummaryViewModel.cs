namespace OptiClick.Core.Install.Summary;

public sealed record InstallSummaryViewModel
{
    public string OptiScalerText { get; init; } = "";
    public string ComponentsText { get; init; } = "";
    public string NoteText { get; init; } = "";
}

using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameMatchResult
{
    public ShellGameMatchStatus Status { get; init; }
    public ShellGameCardModel? Game { get; init; }
    public string MatchedExe { get; init; } = "";
    public string FolderPath { get; init; } = "";
    public string ReasonCode { get; init; } = "";
}

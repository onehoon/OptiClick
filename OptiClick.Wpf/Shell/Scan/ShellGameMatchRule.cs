using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameMatchRule
{
    public string GameId { get; init; } = "";
    public string MatchRuleKey { get; init; } = "";
    public IReadOnlyList<string> RequiredFiles { get; init; } = [];
    public IReadOnlyList<string> ExecutableCandidates { get; init; } = [];
    public ShellGameCardModel Game { get; init; } = new();
}

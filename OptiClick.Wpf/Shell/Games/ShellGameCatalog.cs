namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCatalog
{
    public static readonly ShellGameCatalog Empty = new();

    public IReadOnlyList<ShellGameCardModel> Games { get; init; } = [];
    public IReadOnlyList<OptiClick.Wpf.Shell.Scan.ShellGameMatchRule> MatchRules { get; init; } = [];
}

using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameExeMatchIndex
{
    public static readonly ShellGameExeMatchIndex Empty = new(
        new Dictionary<string, IReadOnlyList<ShellGameCardModel>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlyList<ShellGameMatchRule>>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    public ShellGameExeMatchIndex(
        IReadOnlyDictionary<string, IReadOnlyList<ShellGameCardModel>> gamesByExeName,
        IReadOnlyDictionary<string, IReadOnlyList<ShellGameMatchRule>> rulesByExecutableName,
        IReadOnlySet<string> allowedExeNames)
    {
        GamesByExeName = gamesByExeName
            ?? new Dictionary<string, IReadOnlyList<ShellGameCardModel>>(StringComparer.OrdinalIgnoreCase);
        RulesByExecutableName = rulesByExecutableName
            ?? new Dictionary<string, IReadOnlyList<ShellGameMatchRule>>(StringComparer.OrdinalIgnoreCase);
        AllowedExeNames = allowedExeNames
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<ShellGameCardModel>> GamesByExeName { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<ShellGameMatchRule>> RulesByExecutableName { get; }
    public IReadOnlySet<string> AllowedExeNames { get; }
}

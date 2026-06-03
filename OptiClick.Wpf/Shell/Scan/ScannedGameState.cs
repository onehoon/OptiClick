namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScannedGameState
{
    private readonly Dictionary<string, ShellGameMatchResult> _matchByGameId;
    private readonly Dictionary<string, string> _targetPathByGameId;

    public ScannedGameState()
    {
        _matchByGameId = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
        _targetPathByGameId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, ShellGameMatchResult> MatchByGameId => _matchByGameId;

    public IReadOnlyDictionary<string, string> TargetPathByGameId => _targetPathByGameId;

    public void Clear()
    {
        _matchByGameId.Clear();
        _targetPathByGameId.Clear();
    }

    public bool ContainsGameId(string gameId)
    {
        var normalizedGameId = (gameId ?? "").Trim();
        return !string.IsNullOrWhiteSpace(normalizedGameId)
               && _matchByGameId.ContainsKey(normalizedGameId);
    }

    public bool TryGetTargetPath(string gameId, out string targetPath)
    {
        var normalizedGameId = (gameId ?? "").Trim();
        return _targetPathByGameId.TryGetValue(normalizedGameId, out targetPath!);
    }

    public void ReplaceMatches(IReadOnlyDictionary<string, ShellGameMatchResult> source)
    {
        ReplaceDictionaryEntries(_matchByGameId, source);
    }

    public void ReplaceTargetPaths(IReadOnlyDictionary<string, string> source)
    {
        ReplaceDictionaryEntries(_targetPathByGameId, source);
    }

    private static void ReplaceDictionaryEntries<TKey, TValue>(
        Dictionary<TKey, TValue> target,
        IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        target.Clear();
        foreach (var pair in source)
        {
            target[pair.Key] = pair.Value;
        }
    }
}

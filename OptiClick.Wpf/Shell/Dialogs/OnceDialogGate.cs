namespace OptiClick.Wpf.Shell.Dialogs;

public sealed class OnceDialogGate
{
    private string _lastKey = "";

    public bool TryMarkShown(string key, string fallback = "unknown")
    {
        var normalized = NormalizeStatusCode(key, fallback);
        if (string.Equals(_lastKey, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _lastKey = normalized;
        return true;
    }

    public string LastKey => _lastKey;

    public void Reset()
    {
        _lastKey = "";
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

namespace OptiClick.Wpf.Services;

public interface IAppUpdateVersionComparer
{
    bool IsUpdateAvailable(string currentVersion, string latestVersion);
}

public sealed class AppUpdateVersionComparer : IAppUpdateVersionComparer
{
    public bool IsUpdateAvailable(string currentVersion, string latestVersion)
    {
        var current = ParseVersionParts(currentVersion);
        var latest = ParseVersionParts(latestVersion);
        if (current.Count == 0 || latest.Count == 0)
        {
            return false;
        }

        var count = Math.Max(current.Count, latest.Count);
        for (var i = 0; i < count; i++)
        {
            var currentValue = i < current.Count ? current[i] : 0;
            var latestValue = i < latest.Count ? latest[i] : 0;
            if (latestValue > currentValue)
            {
                return true;
            }

            if (latestValue < currentValue)
            {
                return false;
            }
        }

        return false;
    }

    private static IReadOnlyList<int> ParseVersionParts(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..].Trim();
        }

        var token = normalized.Split(['-', '+'], 2, StringSplitOptions.TrimEntries)[0];
        if (string.IsNullOrWhiteSpace(token))
        {
            return [];
        }

        var rawParts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parts = new List<int>(rawParts.Length);
        foreach (var rawPart in rawParts)
        {
            if (!int.TryParse(rawPart, out var number))
            {
                return [];
            }

            parts.Add(number);
        }

        return parts;
    }
}


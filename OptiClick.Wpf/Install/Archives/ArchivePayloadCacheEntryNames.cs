using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public static class ArchivePayloadCacheEntryNames
{
    public const string OptiPatcherRolling = "Rolling";

    public static string ResolveVersionedEntryName(RemoteArchiveEntry entry, string fallback)
    {
        var candidate = (entry.Version ?? "").Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = Path.GetFileNameWithoutExtension((entry.Filename ?? "").Trim());
        }

        return Normalize(candidate, fallback);
    }

    public static string ResolveOptiScalerEntryName(RemoteArchiveEntry entry)
    {
        var candidate = (entry.Version ?? "").Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = Path.GetFileNameWithoutExtension((entry.Filename ?? "").Trim());
        }

        return Normalize(candidate, "OptiScaler");
    }

    public static string Normalize(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return NormalizeFallback(fallback);
        }

        var chars = value
            .Trim()
            .Select(static ch =>
                (ch is >= 'a' and <= 'z')
                || (ch is >= 'A' and <= 'Z')
                || (ch is >= '0' and <= '9')
                || ch is '.' or '_' or '-'
                    ? ch
                    : '_')
            .ToArray();
        var normalized = new string(chars).Trim('.', '_', '-');
        return string.IsNullOrWhiteSpace(normalized) ? NormalizeFallback(fallback) : normalized;
    }

    private static string NormalizeFallback(string fallback)
    {
        return string.IsNullOrWhiteSpace(fallback) ? "payload" : Normalize(fallback, "payload");
    }
}

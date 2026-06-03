using System.Reflection;

namespace OptiClick.Wpf.Services;

public sealed class AssemblyAppVersionProvider : IAppVersionProvider
{
    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return NormalizeVersion(informationalVersion);
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        var entryInformationalVersion = entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(entryInformationalVersion))
        {
            return NormalizeVersion(entryInformationalVersion);
        }

        var fallback = assembly.GetName().Version?.ToString()
            ?? entryAssembly?.GetName().Version?.ToString();
        return NormalizeVersion(fallback);
    }

    private static string NormalizeVersion(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "0.0.0";
        }

        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..].Trim();
        }

        var token = normalized.Split(['-', '+'], 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrWhiteSpace(token) ? "0.0.0" : token;
    }
}

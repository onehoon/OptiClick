using System.Reflection;
using System.Text.Json;

namespace OptiClick.Wpf.Configuration;

public static class EmbeddedAppSettingsReader
{
    private const string AppSettingsFileName = "appsettings.json";

    public static JsonDocument Load()
    {
        var assembly = typeof(EmbeddedAppSettingsReader).Assembly;
        var resourceName = ResolveResourceName(assembly);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to open embedded resource: {resourceName}");
        }

        return JsonDocument.Parse(stream);
    }

    private static string ResolveResourceName(Assembly assembly)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        var matches = resourceNames
            .Where(name =>
                name.EndsWith($".{AppSettingsFileName}", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, AppSettingsFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"Embedded {AppSettingsFileName} resource was not found in assembly {assembly.GetName().Name}.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple embedded {AppSettingsFileName} resources were found: {string.Join(", ", matches)}");
        }

        return matches[0];
    }
}

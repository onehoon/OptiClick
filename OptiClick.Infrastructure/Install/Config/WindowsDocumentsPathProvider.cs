using Microsoft.Win32;
using System.IO;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class WindowsDocumentsPathProvider : IDocumentsPathProvider
{
    private readonly IEnvironmentPathExpander _environmentPathExpander;

    public WindowsDocumentsPathProvider(IEnvironmentPathExpander? environmentPathExpander = null)
    {
        _environmentPathExpander = environmentPathExpander ?? new EnvironmentPathExpander();
    }

    public IReadOnlyList<string> GetDocumentsCandidates()
    {
        var candidates = new List<string>();

        var registryDocuments = GetWindowsDocumentsDirFromRegistry();
        if (!string.IsNullOrWhiteSpace(registryDocuments))
        {
            candidates.Add(registryDocuments);
        }

        foreach (var envName in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
        {
            var envValue = (Environment.GetEnvironmentVariable(envName) ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                candidates.Add(Path.Combine(envValue, "Documents"));
            }
        }

        var userProfile = (Environment.GetEnvironmentVariable("USERPROFILE") ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            candidates.Add(Path.Combine(userProfile, "Documents"));
        }

        var homeDocuments = Path.Combine(_environmentPathExpander.GetUserHomeDirectory(), "Documents");
        if (!string.IsNullOrWhiteSpace(homeDocuments))
        {
            candidates.Add(homeDocuments);
        }

        return DedupeCandidates(candidates);
    }

    private string? GetWindowsDocumentsDirFromRegistry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var registryTargets = new[]
        {
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
        };

        foreach (var subKey in registryTargets)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey);
                var value = key?.GetValue("Personal")?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var expanded = _environmentPathExpander.ExpandEnvironmentVariables(value);
                if (!string.IsNullOrWhiteSpace(expanded))
                {
                    return expanded.Trim();
                }
            }
            catch
            {
                // Keep best-effort behavior aligned with Python candidate probing.
            }
        }

        return null;
    }

    private IReadOnlyList<string> DedupeCandidates(IEnumerable<string> candidates)
    {
        var uniqueCandidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var normalized = NormalizeCandidatePath(candidate);
            if (!seen.Add(normalized))
            {
                continue;
            }

            uniqueCandidates.Add(candidate);
        }

        return uniqueCandidates;
    }

    private string NormalizeCandidatePath(string candidate)
    {
        var expanded = _environmentPathExpander.ExpandUserHome(candidate ?? "");
        try
        {
            return Path.GetFullPath(expanded).ToLowerInvariant();
        }
        catch
        {
            return expanded.ToLowerInvariant();
        }
    }
}

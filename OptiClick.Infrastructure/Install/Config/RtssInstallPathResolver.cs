using System.IO;
using Microsoft.Win32;

namespace OptiClick.Infrastructure.Install.Config;

internal static class RtssInstallPathResolver
{
    private static readonly RegistryHive[] RegistryRoots = [RegistryHive.LocalMachine, RegistryHive.CurrentUser];
    private static readonly RegistryView[] RegistryViews = [RegistryView.Registry64, RegistryView.Registry32];
    private static readonly string[] RegistrySubKeys = [@"SOFTWARE\WOW6432Node\Unwinder\RTSS", @"SOFTWARE\Unwinder\RTSS"];

    public static string Resolve()
    {
        if (OperatingSystem.IsWindows())
        {
            foreach (var root in RegistryRoots)
            {
                foreach (var view in RegistryViews)
                {
                    foreach (var subKey in RegistrySubKeys)
                    {
                        try
                        {
                            using var baseKey = RegistryKey.OpenBaseKey(root, view);
                            using var key = baseKey.OpenSubKey(subKey, writable: false);
                            var rawPath = key?.GetValue("InstallPath")?.ToString();
                            var normalized = NormalizeInstallPath(rawPath);
                            if (!string.IsNullOrWhiteSpace(normalized) && Directory.Exists(normalized))
                            {
                                return normalized;
                            }
                        }
                        catch
                        {
                            // Ignore registry read failures.
                        }
                    }
                }
            }
        }

        var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        var fallbackRoot = string.IsNullOrWhiteSpace(programFilesX86) ? @"C:\Program Files (x86)" : programFilesX86.Trim();
        return Path.Combine(fallbackRoot, "RivaTuner Statistics Server");
    }

    private static string NormalizeInstallPath(string? path)
    {
        var normalized = (path ?? "").Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (File.Exists(normalized) && string.Equals(Path.GetFileName(normalized), "RTSS.exe", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(normalized) ?? "";
        }

        return normalized;
    }
}

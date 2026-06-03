using System.IO;
using Microsoft.Win32;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

public interface IRtssOverlayNoticeResolver
{
    string ResolveNotice(ShellGameCardModel game, AppLanguage language);
}

public sealed class RtssOverlayNoticeResolver : IRtssOverlayNoticeResolver
{
    private static readonly RegistryHive[] RegistryRoots =
    [
        RegistryHive.LocalMachine,
        RegistryHive.CurrentUser
    ];

    private static readonly RegistryView[] RegistryViews =
    [
        RegistryView.Registry64,
        RegistryView.Registry32
    ];

    private static readonly string[] RegistrySubKeys =
    [
        @"SOFTWARE\WOW6432Node\Unwinder\RTSS",
        @"SOFTWARE\Unwinder\RTSS"
    ];

    private const string KoreanRtssOverlayNotice = "[RED]선택한 게임에 설치 시 RTSS 오버레이가 Off 됩니다. (충돌 방지)[END]";
    private const string EnglishRtssOverlayNotice = "[RED]RTSS overlay may cause issues with this game.[br]It will be turned off for this game during installation.[END]";

    public string ResolveNotice(ShellGameCardModel game, AppLanguage language)
    {
        if (game is null || !game.RtssOverlay)
        {
            return "";
        }

        return IsRtssOverlayNoticeRequired()
            ? language == AppLanguage.Korean ? KoreanRtssOverlayNotice : EnglishRtssOverlayNotice
            : "";
    }

    private static bool IsRtssOverlayNoticeRequired()
    {
        var installPath = ResolveRtssInstallPath();
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }

        var rtssExePath = Path.Combine(installPath, "RTSS.exe");
        if (!File.Exists(rtssExePath))
        {
            return false;
        }

        var globalProfilePath = Path.Combine(installPath, "Profiles", "Global");
        return File.Exists(globalProfilePath);
    }

    private static string ResolveRtssInstallPath()
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
                            if (string.IsNullOrWhiteSpace(normalized))
                            {
                                continue;
                            }

                            if (Directory.Exists(normalized))
                            {
                                return normalized;
                            }
                        }
                        catch
                        {
                            // Ignore registry read failures and continue fallback probing.
                        }
                    }
                }
            }
        }

        var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        var fallbackRoot = string.IsNullOrWhiteSpace(programFilesX86)
            ? @"C:\Program Files (x86)"
            : programFilesX86.Trim();
        return Path.Combine(fallbackRoot, "RivaTuner Statistics Server");
    }

    private static string NormalizeInstallPath(string? path)
    {
        var normalized = (path ?? "").Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (File.Exists(normalized)
            && string.Equals(Path.GetFileName(normalized), "RTSS.exe", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(normalized) ?? "";
        }

        return normalized;
    }
}

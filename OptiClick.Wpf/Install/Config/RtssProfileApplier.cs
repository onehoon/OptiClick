using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.Win32;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Install.Config;

public interface IRtssProfileApplier
{
    RtssProfileApplyResult Apply(ShellGameCardModel? game, ShellGameMatchResult? selectedMatch);
}

public sealed record RtssProfileApplyResult
{
    public IReadOnlyList<InstallFlowLogEntry> Logs { get; init; } = Array.Empty<InstallFlowLogEntry>();
}

public sealed class RtssProfileApplier : IRtssProfileApplier
{
    private static readonly RegistryHive[] RegistryRoots = [RegistryHive.LocalMachine, RegistryHive.CurrentUser];
    private static readonly RegistryView[] RegistryViews = [RegistryView.Registry64, RegistryView.Registry32];
    private static readonly string[] RegistrySubKeys = [@"SOFTWARE\WOW6432Node\Unwinder\RTSS", @"SOFTWARE\Unwinder\RTSS"];

    public RtssProfileApplyResult Apply(ShellGameCardModel? game, ShellGameMatchResult? selectedMatch)
    {
        var logs = new List<InstallFlowLogEntry>();
        try
        {
            var installPath = ResolveRtssInstallPath();
            var globalPath = Path.Combine(installPath, "Profiles", "Global");
            if (!File.Exists(globalPath))
            {
                return new RtssProfileApplyResult { Logs = logs };
            }

            var globalChanged = ApplyGlobalSettings(globalPath, logs);
            var gameProfileChanged = ApplyGameProfileOverlay(game, selectedMatch, installPath, globalPath, logs);
            var restarted = false;
            if (globalChanged || gameProfileChanged)
            {
                restarted = RestartRtssSilently(installPath, logs);
                logs.Add(Info($"[RTSS] completed global_changed={FormatBool(globalChanged)} game_profile_changed={FormatBool(gameProfileChanged)} restarted={FormatBool(restarted)}"));
            }
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"[RTSS] Failed to apply RTSS settings: {ex.Message}"));
        }

        return new RtssProfileApplyResult { Logs = logs };
    }

    private static bool ApplyGlobalSettings(string globalPath, List<InstallFlowLogEntry> logs)
    {
        try
        {
            var (text, encoding, bom) = DecodeText(File.ReadAllBytes(globalPath));
            var lineEnding = DetectLineEnding(text);
            var hadTrailingNewline = text.EndsWith('\n') || text.EndsWith('\r');
            var lines = SplitLines(text);

            string? reflex = null;
            string? detours = null;
            var updated = new List<string>(lines.Count + 2);
            var hasReflex = false;
            var hasDetours = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains('='))
                {
                    updated.Add(line);
                    continue;
                }

                var separator = trimmed.IndexOf('=');
                var key = trimmed[..separator].Trim();
                var value = trimmed[(separator + 1)..].Trim();
                if (string.Equals(key, "ReflexSetLatencyMarker", StringComparison.Ordinal))
                {
                    reflex = value;
                    updated.Add("ReflexSetLatencyMarker=0");
                    hasReflex = true;
                    continue;
                }

                if (string.Equals(key, "UseDetours", StringComparison.Ordinal))
                {
                    detours = value;
                    updated.Add("UseDetours=1");
                    hasDetours = true;
                    continue;
                }

                updated.Add(line);
            }

            if (string.Equals(reflex, "0", StringComparison.Ordinal) && string.Equals(detours, "1", StringComparison.Ordinal))
            {
                return false;
            }

            if (!hasReflex)
            {
                updated.Add("ReflexSetLatencyMarker=0");
            }

            if (!hasDetours)
            {
                updated.Add("UseDetours=1");
            }

            var newText = string.Join(lineEnding, updated);
            if (hadTrailingNewline)
            {
                newText += lineEnding;
            }

            WithTemporaryWriteAccess(globalPath, logs, () =>
            {
                File.WriteAllBytes(globalPath, EncodeText(newText, encoding, bom));
            });
            return true;
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"[RTSS] Failed to apply Global settings fix: {ex.Message}"));
            return false;
        }
    }

    private static bool ApplyGameProfileOverlay(
        ShellGameCardModel? game,
        ShellGameMatchResult? selectedMatch,
        string installPath,
        string globalPath,
        List<InstallFlowLogEntry> logs)
    {
        try
        {
            if (game is null || !ShellGameInstallMetadataResolver.GetRtssOverlay(game))
            {
                return false;
            }

            var exeName = ResolveExeName(game, selectedMatch);
            if (string.IsNullOrWhiteSpace(exeName))
            {
                return false;
            }

            var profilesDir = Path.Combine(installPath, "Profiles");
            var profilePath = Path.Combine(profilesDir, $"{exeName}.cfg");
            if (!File.Exists(profilePath))
            {
                Directory.CreateDirectory(profilesDir);
                File.Copy(globalPath, profilePath, overwrite: false);
            }

            WithTemporaryWriteAccess(profilePath, logs, () =>
            {
                var (text, encoding, bom) = DecodeText(File.ReadAllBytes(profilePath));
                var updated = ApplyIniSectionKeyValues(text, new Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OSD"] = [new("EnableOSD", "0")],
                    ["Hooking"] =
                    [
                        new("EnableHooking", "0"),
                        new("HookDirect3D8", "0"),
                        new("HookDirect3D9", "0"),
                        new("HookDXGI", "0"),
                        new("HookDirect3D12", "0"),
                        new("HookOpenGL", "0"),
                        new("HookVulkan", "0"),
                        new("UseDetours", "1")
                    ],
                    ["Framerate"] = [new("ReflexSetLatencyMarker", "0")]
                });
                File.WriteAllBytes(profilePath, EncodeText(updated, encoding, bom));
            });
            return true;
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"[RTSS] Failed to apply game profile overlay: {ex.Message}"));
            return false;
        }
    }

    private static string ResolveExeName(ShellGameCardModel game, ShellGameMatchResult? selectedMatch)
    {
        var matchedExe = (selectedMatch?.MatchedExe ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(matchedExe))
        {
            return Path.GetFileName(matchedExe);
        }

        var requiredFiles = MatchExePatternParser.ParseRequiredFiles(game.MatchExe);
        var executableCandidates = MatchExePatternParser.ExtractExecutableCandidates(requiredFiles);
        if (executableCandidates.Count > 0)
        {
            return Path.GetFileName(executableCandidates[0]);
        }

        var fallback = (game.MatchExe ?? "").Trim();
        return string.IsNullOrWhiteSpace(fallback) ? "" : Path.GetFileName(fallback);
    }

    private static bool RestartRtssSilently(string installPath, List<InstallFlowLogEntry> logs)
    {
        try
        {
            var rtssExePath = Path.Combine(installPath, "RTSS.exe");
            if (!File.Exists(rtssExePath))
            {
                return false;
            }

            foreach (var process in Process.GetProcessesByName("RTSS"))
            {
                process.Kill(entireProcessTree: false);
                process.WaitForExit(2000);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = rtssExePath,
                WorkingDirectory = installPath,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return true;
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"[RTSS] Failed to restart RTSS silently: {ex.Message}"));
            return false;
        }
    }

    private static void WithTemporaryWriteAccess(string filePath, List<InstallFlowLogEntry> logs, Action action)
    {
        var attributes = File.GetAttributes(filePath);
        var wasReadOnly = attributes.HasFlag(FileAttributes.ReadOnly);
        try
        {
            if (wasReadOnly)
            {
                File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            }

            action();
        }
        finally
        {
            if (wasReadOnly)
            {
                File.SetAttributes(filePath, attributes);
            }
        }
    }

    private static (string Text, Encoding Encoding, byte[] Bom) DecodeText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), Encoding.UTF8, [0xEF, 0xBB, 0xBF]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode, [0xFF, 0xFE]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), Encoding.BigEndianUnicode, [0xFE, 0xFF]);
        }

        try
        {
            return (new UTF8Encoding(false, true).GetString(bytes), Encoding.UTF8, []);
        }
        catch
        {
            return (Encoding.Latin1.GetString(bytes), Encoding.Latin1, []);
        }
    }

    private static byte[] EncodeText(string text, Encoding encoding, byte[] bom)
    {
        var body = encoding.GetBytes(text);
        if (bom.Length == 0)
        {
            return body;
        }

        return bom.Concat(body).ToArray();
    }

    private static string ApplyIniSectionKeyValues(
        string text,
        IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>> sectionTargets)
    {
            var lineEnding = DetectLineEnding(text);
            var hadTrailingNewline = text.EndsWith('\n') || text.EndsWith('\r');
            var lines = SplitLines(text).ToList();
        var sectionOrder = sectionTargets.Keys.ToArray();
        var foundSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var applied = sectionTargets.Keys.ToDictionary(static key => key, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

        var output = new List<string>(lines.Count + 32);
        string currentSection = "";
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length >= 3)
            {
                currentSection = trimmed[1..^1].Trim();
                if (sectionTargets.ContainsKey(currentSection))
                {
                    foundSections.Add(currentSection);
                }

                output.Add(line);
                continue;
            }

            if (!trimmed.Contains('=') || string.IsNullOrWhiteSpace(currentSection) || !sectionTargets.TryGetValue(currentSection, out var sectionPairs))
            {
                output.Add(line);
                continue;
            }

            var separator = line.IndexOf('=');
            var keyRaw = separator >= 0 ? line[..separator] : line;
            var key = keyRaw.Trim();
            var matched = sectionPairs.FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(matched.Key))
            {
                output.Add(line);
                continue;
            }

            output.Add($"{key}={matched.Value}");
            applied[currentSection].Add(matched.Key);
        }

        foreach (var section in sectionOrder)
        {
            if (!foundSections.Contains(section))
            {
                output.Add($"[{section}]");
                foreach (var pair in sectionTargets[section])
                {
                    output.Add($"{pair.Key}={pair.Value}");
                }

                continue;
            }

            foreach (var pair in sectionTargets[section])
            {
                if (!applied[section].Contains(pair.Key))
                {
                    output.Add($"{pair.Key}={pair.Value}");
                }
            }
        }

        var updated = string.Join(lineEnding, output);
        if (hadTrailingNewline)
        {
            updated += lineEnding;
        }

        return updated;
    }

    private static string DetectLineEnding(string text)
    {
        if (text.Contains("\r\n", StringComparison.Ordinal))
        {
            return "\r\n";
        }

        if (text.Contains('\r'))
        {
            return "\r";
        }

        return "\n";
    }

    private static IReadOnlyList<string> SplitLines(string text)
    {
        return (text ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
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

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    private static InstallFlowLogEntry Info(string message)
    {
        return new InstallFlowLogEntry { Level = "info", Category = "config", Message = message };
    }

    private static InstallFlowLogEntry Warning(string message)
    {
        return new InstallFlowLogEntry { Level = "warning", Category = "config", Message = message };
    }
}

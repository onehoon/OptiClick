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

internal sealed record RtssRestartResult
{
    public bool Restarted { get; init; }
    public string Error { get; init; } = "";
}

public sealed class RtssProfileApplier : IRtssProfileApplier
{
    private static readonly RegistryHive[] RegistryRoots = [RegistryHive.LocalMachine, RegistryHive.CurrentUser];
    private static readonly RegistryView[] RegistryViews = [RegistryView.Registry64, RegistryView.Registry32];
    private static readonly string[] RegistrySubKeys = [@"SOFTWARE\WOW6432Node\Unwinder\RTSS", @"SOFTWARE\Unwinder\RTSS"];

    private readonly Func<string> _installPathResolver;
    private readonly Func<string, RtssRestartResult> _restartRtss;

    public RtssProfileApplier()
        : this(ResolveRtssInstallPath, RestartRtssSilently)
    {
    }

    internal RtssProfileApplier(
        Func<string> installPathResolver,
        Func<string, RtssRestartResult> restartRtss)
    {
        _installPathResolver = installPathResolver ?? throw new ArgumentNullException(nameof(installPathResolver));
        _restartRtss = restartRtss ?? throw new ArgumentNullException(nameof(restartRtss));
    }

    public RtssProfileApplyResult Apply(ShellGameCardModel? game, ShellGameMatchResult? selectedMatch)
    {
        var logs = new List<InstallFlowLogEntry>();
        try
        {
            var installPath = _installPathResolver();
            var globalPath = Path.Combine(installPath, "Profiles", "Global");
            if (!File.Exists(globalPath))
            {
                return new RtssProfileApplyResult { Logs = logs };
            }

            var globalResult = ApplyGlobalSettings(globalPath);
            foreach (var item in globalResult.Items)
            {
                logs.Add(Info(FormatGlobalItemLog(item)));
            }

            var profileOutcome = ApplyGameProfileOverlay(game, selectedMatch, installPath, globalPath);
            var restart = globalResult.Changed || profileOutcome.ProfileChanged
                ? _restartRtss(installPath)
                : new RtssRestartResult();
            logs.Add(Info(FormatProfileLog(profileOutcome, restart)));
        }
        catch (Exception ex)
        {
            logs.Add(Warning($"[RTSS] Failed to apply RTSS settings: {ex.Message}"));
        }

        return new RtssProfileApplyResult { Logs = logs };
    }

    private sealed record RtssGlobalApplyResult
    {
        public IReadOnlyList<RtssGlobalItemLog> Items { get; init; } = Array.Empty<RtssGlobalItemLog>();
        public bool Changed { get; init; }
    }

    private sealed record RtssGlobalItemLog
    {
        public string Key { get; init; } = "";
        public string Status { get; init; } = "";
        public string Reason { get; init; } = "";
        public string OldValue { get; init; } = "";
        public string NewValue { get; init; } = "";
        public string Detail { get; init; } = "";
    }

    private sealed record RtssGameProfileApplyOutcome
    {
        public string Status { get; init; } = "";
        public string Reason { get; init; } = "";
        public string Action { get; init; } = "";
        public string ExeName { get; init; } = "";
        public bool ProfileChanged { get; init; }
        public string Detail { get; init; } = "";
    }

    private static RtssGlobalApplyResult ApplyGlobalSettings(string globalPath)
    {
        try
        {
            var (text, encoding, bom) = DecodeText(File.ReadAllBytes(globalPath));
            var lineEnding = DetectLineEnding(text);
            var hadTrailingNewline = text.EndsWith('\n') || text.EndsWith('\r');
            var lines = SplitLines(text);

            string? reflex = null;
            string? detours = null;
            var reflexChanged = false;
            var detoursChanged = false;
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
                    reflexChanged |= !string.Equals(value, "0", StringComparison.Ordinal);
                    updated.Add("ReflexSetLatencyMarker=0");
                    hasReflex = true;
                    continue;
                }

                if (string.Equals(key, "UseDetours", StringComparison.Ordinal))
                {
                    detours = value;
                    detoursChanged |= !string.Equals(value, "1", StringComparison.Ordinal);
                    updated.Add("UseDetours=1");
                    hasDetours = true;
                    continue;
                }

                updated.Add(line);
            }

            if (!hasReflex)
            {
                reflexChanged = true;
                updated.Add("ReflexSetLatencyMarker=0");
            }

            if (!hasDetours)
            {
                detoursChanged = true;
                updated.Add("UseDetours=1");
            }

            var changed = reflexChanged || detoursChanged;
            var items = new[]
            {
                CreateGlobalItem("ReflexSetLatencyMarker", hasReflex ? reflex ?? "" : "<missing>", "0", reflexChanged),
                CreateGlobalItem("UseDetours", hasDetours ? detours ?? "" : "<missing>", "1", detoursChanged)
            };

            if (!changed)
            {
                return new RtssGlobalApplyResult
                {
                    Items = items,
                    Changed = false
                };
            }

            var newText = string.Join(lineEnding, updated);
            if (hadTrailingNewline)
            {
                newText += lineEnding;
            }

            WithTemporaryWriteAccess(globalPath, () =>
            {
                File.WriteAllBytes(globalPath, EncodeText(newText, encoding, bom));
            });
            return new RtssGlobalApplyResult
            {
                Items = items,
                Changed = true
            };
        }
        catch (Exception ex)
        {
            return new RtssGlobalApplyResult
            {
                Items =
                [
                    CreateGlobalErrorItem("ReflexSetLatencyMarker", "0", ex.Message),
                    CreateGlobalErrorItem("UseDetours", "1", ex.Message)
                ],
                Changed = false
            };
        }
    }

    private static RtssGameProfileApplyOutcome ApplyGameProfileOverlay(
        ShellGameCardModel? game,
        ShellGameMatchResult? selectedMatch,
        string installPath,
        string globalPath)
    {
        try
        {
            if (game is null)
            {
                return new RtssGameProfileApplyOutcome
                {
                    Status = "not_applicable",
                    Reason = "game_missing"
                };
            }

            if (!ShellGameInstallMetadataResolver.GetRtssOverlay(game))
            {
                return new RtssGameProfileApplyOutcome
                {
                    Status = "not_applicable",
                    Reason = "rtss_overlay_false"
                };
            }

            var exeName = ResolveExeName(game, selectedMatch);
            if (string.IsNullOrWhiteSpace(exeName))
            {
                return new RtssGameProfileApplyOutcome
                {
                    Status = "skipped",
                    Reason = "exe_missing"
                };
            }

            var profilesDir = Path.Combine(installPath, "Profiles");
            var profilePath = Path.Combine(profilesDir, $"{exeName}.cfg");
            var created = false;
            if (!File.Exists(profilePath))
            {
                Directory.CreateDirectory(profilesDir);
                File.Copy(globalPath, profilePath, overwrite: false);
                created = true;
            }

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

            if (created || !string.Equals(text, updated, StringComparison.Ordinal))
            {
                WithTemporaryWriteAccess(profilePath, () =>
                {
                    File.WriteAllBytes(profilePath, EncodeText(updated, encoding, bom));
                });

                return new RtssGameProfileApplyOutcome
                {
                    Status = "applied",
                    Action = created ? "created" : "updated",
                    ExeName = exeName,
                    ProfileChanged = true
                };
            }

            return new RtssGameProfileApplyOutcome
            {
                Status = "skipped",
                Reason = "unchanged",
                Action = "none",
                ExeName = exeName
            };
        }
        catch (Exception ex)
        {
            return new RtssGameProfileApplyOutcome
            {
                Status = "error",
                Reason = "apply_failed",
                Detail = ex.Message
            };
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

    private static RtssGlobalItemLog CreateGlobalItem(string key, string oldValue, string newValue, bool changed)
    {
        return new RtssGlobalItemLog
        {
            Key = key,
            Status = changed ? "applied" : "skipped",
            Reason = changed ? "" : "unchanged",
            OldValue = oldValue,
            NewValue = newValue
        };
    }

    private static RtssGlobalItemLog CreateGlobalErrorItem(string key, string newValue, string detail)
    {
        return new RtssGlobalItemLog
        {
            Key = key,
            Status = "error",
            Reason = "apply_failed",
            OldValue = "<unread>",
            NewValue = newValue,
            Detail = detail
        };
    }

    private static string FormatGlobalItemLog(RtssGlobalItemLog item)
    {
        var reason = string.IsNullOrWhiteSpace(item.Reason) ? "" : $" reason={NormalizeStatusCode(item.Reason, "unknown")}";
        var detail = string.IsNullOrWhiteSpace(item.Detail) ? "" : $" detail={Quote(item.Detail)}";
        return $"[RTSS] global item status={NormalizeStatusCode(item.Status, "unknown")}{reason} key={Quote(item.Key)} old={Quote(item.OldValue)} new={Quote(item.NewValue)}{detail}";
    }

    private static string FormatProfileLog(RtssGameProfileApplyOutcome outcome, RtssRestartResult restart)
    {
        var reason = string.IsNullOrWhiteSpace(outcome.Reason) ? "" : $" reason={NormalizeStatusCode(outcome.Reason, "unknown")}";
        var action = string.IsNullOrWhiteSpace(outcome.Action) ? "" : $" action={NormalizeStatusCode(outcome.Action, "none")}";
        var exe = string.IsNullOrWhiteSpace(outcome.ExeName) ? "" : $" exe={Quote(outcome.ExeName)}";
        var restartError = string.IsNullOrWhiteSpace(restart.Error) ? "" : $" restart_error={Quote(restart.Error)}";
        var detail = string.IsNullOrWhiteSpace(outcome.Detail) ? "" : $" detail={Quote(outcome.Detail)}";
        return $"[RTSS] profile status={NormalizeStatusCode(outcome.Status, "unknown")}{reason}{action}{exe} profile_changed={FormatBool(outcome.ProfileChanged)} restarted={FormatBool(restart.Restarted)}{restartError}{detail}";
    }

    private static RtssRestartResult RestartRtssSilently(string installPath)
    {
        try
        {
            var rtssExePath = Path.Combine(installPath, "RTSS.exe");
            if (!File.Exists(rtssExePath))
            {
                return new RtssRestartResult
                {
                    Restarted = false,
                    Error = "rtss_exe_missing"
                };
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
            return new RtssRestartResult
            {
                Restarted = true
            };
        }
        catch (Exception ex)
        {
            return new RtssRestartResult
            {
                Restarted = false,
                Error = ex.Message
            };
        }
    }

    private static void WithTemporaryWriteAccess(string filePath, Action action)
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

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string Quote(string value)
    {
        var safeValue = value ?? "";
        var escaped = safeValue
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return $"\"{escaped}\"";
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

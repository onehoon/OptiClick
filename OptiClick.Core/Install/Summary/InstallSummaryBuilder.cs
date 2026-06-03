using System.Text.RegularExpressions;

namespace OptiClick.Core.Install.Summary;

public static partial class InstallSummaryBuilder
{
    private static readonly string[] SummaryMarkupLineBreakTags = ["[BR]", "[P]"];
    private static readonly string[] SummaryMarkupRemoveTokens = ["[RED]", "[END]", "[DOT]", "[INDENT]"];

    public static InstallSummaryViewModel Build(InstallSummaryInput input)
    {
        return Build(input, new InstallSummaryStrings());
    }

    public static InstallSummaryViewModel Build(InstallSummaryInput input, InstallSummaryStrings strings)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(strings);

        return new InstallSummaryViewModel
        {
            OptiScalerText = BuildOptiScalerSummaryText(input, strings),
            ComponentsText = BuildComponentsText(input, strings),
            NoteText = StripSummaryNoteMarkup(input.InstallSummaryNote)
        };
    }

    private static string BuildOptiScalerSummaryText(InstallSummaryInput input, InstallSummaryStrings strings)
    {
        var statusCode = (input.InstallStatusCode ?? "").Trim();
        var installedVersion = FormatVersion(input.InstalledVersion);
        var latestVersion = FormatVersion(PickLatestVersionText(input));

        if (statusCode.Equals(InstallSummaryStatusCodes.UpdateAvailable, StringComparison.OrdinalIgnoreCase))
        {
            return BuildActionLine(strings.ActionUpdate, strings.AutoConfigApplied, installedVersion, latestVersion);
        }

        if (statusCode.Equals(InstallSummaryStatusCodes.Installable, StringComparison.OrdinalIgnoreCase))
        {
            return BuildActionLine(strings.ActionInstall, strings.AutoConfigApplied, latestVersion);
        }

        return BuildActionLine(strings.ActionReinstall, strings.AutoConfigApplied, installedVersion);
    }

    private static string BuildComponentsText(InstallSummaryInput input, InstallSummaryStrings strings)
    {
        var components = new List<string>();

        if (input.OptiPatcher)
        {
            components.Add(strings.ComponentOptiPatcher);
        }

        if (input.Unreal5)
        {
            components.Add(strings.ComponentUnreal5);
        }

        if (!string.IsNullOrWhiteSpace(input.ReframeworkUrl))
        {
            components.Add(strings.ComponentReframework);
        }

        if (input.UltimateAsiLoader)
        {
            components.Add(strings.ComponentUltimateAsiLoader);
        }

        if (!string.IsNullOrWhiteSpace(input.SpecialK))
        {
            components.Add(strings.ComponentSpecialK);
        }

        if (input.RtssOverlay)
        {
            components.Add(strings.ComponentRtssOverlay);
        }

        return components.Count == 0 ? "" : string.Join(", ", components);
    }

    private static string PickLatestVersionText(InstallSummaryInput input)
    {
        var displayVersion = CleanVersionText(input.CurrentDisplayVersion);
        return !string.IsNullOrEmpty(displayVersion) ? displayVersion : CleanVersionText(input.CurrentVersion);
    }

    private static string FormatVersion(string? value)
    {
        var version = CleanVersionText(value);
        if (string.IsNullOrEmpty(version))
        {
            return "";
        }

        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
    }

    private static string CleanVersionText(string? value)
    {
        return (value ?? "").Trim();
    }

    private static string BuildActionLine(string actionText, string autoConfigText, params string[] versions)
    {
        var normalizedAction = (actionText ?? "").Trim();
        var normalizedAutoConfig = (autoConfigText ?? "").Trim();
        var filteredVersions = versions.Where(static version => !string.IsNullOrWhiteSpace(version)).ToArray();

        if (filteredVersions.Length == 0)
        {
            return $"{normalizedAction}, {normalizedAutoConfig}";
        }

        if (filteredVersions.Length == 1)
        {
            return $"{normalizedAction} ({filteredVersions[0]}), {normalizedAutoConfig}";
        }

        return $"{normalizedAction} ({filteredVersions[0]} -> {filteredVersions[1]}), {normalizedAutoConfig}";
    }

    private static string StripSummaryNoteMarkup(string? rawText)
    {
        var text = rawText ?? "";
        foreach (var token in SummaryMarkupLineBreakTags)
        {
            text = text.Replace(token, " ", StringComparison.Ordinal);
        }

        foreach (var token in SummaryMarkupRemoveTokens)
        {
            text = text.Replace(token, "", StringComparison.Ordinal);
        }

        text = SummaryMarkupPattern().Replace(text, "");
        text = WhitespacePattern().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex(@"\[[A-Z_]+\]")]
    private static partial Regex SummaryMarkupPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public sealed class AppUpdateDialogPresenter
{
    public AppDialogRequest BuildNoUpdateDialog(AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateCheckTitle,
            Summary = text.UpdateLatestMessage,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info
        };
    }

    public AppDialogRequest BuildUpdateAvailableDialog(AppUpdateInfo updateInfo, AppUpdateFlowText text, AppLanguage language)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        return new AppDialogRequest
        {
            Title = text.UpdateAvailableTitle,
            Summary = text.UpdateAvailableSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems = ParseReleaseNotes(updateInfo.Notes, language),
            PrimaryButtonText = text.UpdateAvailablePrimaryButton,
            SecondaryButtonText = text.UpdateAvailableSecondaryButton,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    public AppDialogRequest BuildUpdateFailedDialog(AppUpdateFlowText text)
    {
        return new AppDialogRequest
        {
            Title = text.UpdateFailedTitle,
            Summary = text.UpdatePrepareFailedSummary,
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            BulletItems = [text.UpdateTryAgainLater]
        };
    }

    private static IReadOnlyList<string> ParseReleaseNotes(string notesMarkdown, AppLanguage language)
    {
        if (string.IsNullOrWhiteSpace(notesMarkdown))
        {
            return [];
        }

        var sections = SplitIntoLanguageSections(notesMarkdown);
        var sectionKey = language == AppLanguage.Korean ? "ko-kr" : "en-us";
        if (sections.TryGetValue(sectionKey, out var sectionBody))
        {
            return ParseBulletLines(sectionBody);
        }

        // No matching ko-KR/en-US section (either no sections at all, or a differently-
        // labeled one) - fall back to showing the whole thing rather than nothing.
        return ParseBulletLines(notesMarkdown);
    }

    private static Dictionary<string, string> SplitIntoLanguageSections(string notesMarkdown)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;
        var currentBody = new List<string>();

        void Flush()
        {
            if (currentKey is not null)
            {
                sections[currentKey] = string.Join('\n', currentBody);
            }
        }

        foreach (var rawLine in notesMarkdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                Flush();
                currentKey = trimmed.TrimStart('#').Trim();
                currentBody = [];
                continue;
            }

            currentBody.Add(line);
        }

        Flush();
        return sections;
    }

    private static IReadOnlyList<string> ParseBulletLines(string markdown)
    {
        return markdown
            .Split('\n')
            .Select(static line => line.Trim().TrimStart('-', '*').Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .ToArray();
    }
}

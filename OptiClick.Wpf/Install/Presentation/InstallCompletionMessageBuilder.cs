using System.Globalization;

namespace OptiClick.Wpf.Install.Presentation;

public sealed class InstallCompletionMessageBuilder
{
    public string BuildAfterInstallPopupMessage(string baseMessage, string? installPostMessage)
    {
        var normalizedBase = (baseMessage ?? "").Trim();
        var normalizedInstallPostMessage = (installPostMessage ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedInstallPostMessage))
        {
            return normalizedBase;
        }

        if (string.IsNullOrWhiteSpace(normalizedBase))
        {
            return normalizedInstallPostMessage;
        }

        return $"{normalizedBase}[P]{normalizedInstallPostMessage}";
    }

    public string BuildInstallCompletionMessage(
        string? installedFileName,
        string installCompleted,
        string installCompletedWithNameTemplate)
    {
        var normalizedName = (installedFileName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return installCompleted;
        }

        return FormatTemplateWithName(installCompletedWithNameTemplate, normalizedName);
    }

    public string BuildInstallPostCompletionMessage(
        string? installedFileName,
        string installPostCompletedWithNameTemplate)
    {
        var normalizedName = (installedFileName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return "";
        }

        return FormatTemplateWithName(installPostCompletedWithNameTemplate, normalizedName);
    }

    private static string FormatTemplateWithName(string? template, string normalizedName)
    {
        var normalizedTemplate = (template ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedTemplate))
        {
            return "";
        }

        if (normalizedTemplate.Contains("{name}", StringComparison.Ordinal))
        {
            return normalizedTemplate.Replace("{name}", normalizedName, StringComparison.Ordinal);
        }

        return string.Format(CultureInfo.CurrentCulture, normalizedTemplate, normalizedName);
    }
}

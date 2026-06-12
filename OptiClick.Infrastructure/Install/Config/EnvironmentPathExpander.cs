using System.IO;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class EnvironmentPathExpander : IEnvironmentPathExpander
{
    public string ExpandEnvironmentVariables(string value)
    {
        return Environment.ExpandEnvironmentVariables(value ?? "");
    }

    public string ExpandUserHome(string value)
    {
        var text = value ?? "";
        if (!text.StartsWith('~'))
        {
            return text;
        }

        if (text.Length == 1)
        {
            return GetUserHomeDirectory();
        }

        var next = text[1];
        if (next is not ('\\' or '/'))
        {
            return text;
        }

        var remainder = text[2..];
        var home = GetUserHomeDirectory();
        if (string.IsNullOrWhiteSpace(home))
        {
            return text;
        }

        return Path.Combine(home, remainder);
    }

    public string GetUserHomeDirectory()
    {
        var userProfile = (Environment.GetEnvironmentVariable("USERPROFILE") ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return userProfile;
        }

        var specialFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(specialFolderPath))
        {
            return specialFolderPath;
        }

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsPath))
        {
            return "";
        }

        var parent = Directory.GetParent(documentsPath);
        return parent?.FullName ?? "";
    }
}

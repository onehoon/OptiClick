using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Support;

public sealed class SupportActionController
{
    private readonly IContactIssueLinkBuilder _contactIssueLinkBuilder;
    private readonly IExternalUrlLauncher _externalUrlLauncher;
    private readonly ILogFolderLauncher _logFolderLauncher;

    public SupportActionController(
        IContactIssueLinkBuilder contactIssueLinkBuilder,
        IExternalUrlLauncher externalUrlLauncher,
        ILogFolderLauncher? logFolderLauncher = null)
    {
        _contactIssueLinkBuilder = contactIssueLinkBuilder ?? throw new ArgumentNullException(nameof(contactIssueLinkBuilder));
        _externalUrlLauncher = externalUrlLauncher ?? throw new ArgumentNullException(nameof(externalUrlLauncher));
        _logFolderLauncher = logFolderLauncher ?? new WindowsLogFolderLauncher();
    }

    public SupportActionResult OpenSupportRequest(
        ContactIssueContext context,
        AppLanguage language,
        AppStrings strings)
    {
        var url = _contactIssueLinkBuilder.BuildIssueUrl(context, language);
        if (_externalUrlLauncher.OpenUrl(url))
        {
            return new SupportActionResult
            {
                IsSuccess = true,
                StatusText = strings.SettingsOpenedGitHubSupportRequest,
                LogCategory = "support",
                LogMessage = "github support request opened"
            };
        }

        return new SupportActionResult
        {
            IsSuccess = false,
            StatusText = strings.SettingsFailedOpenGitHubSupportRequest,
            LogCategory = "support",
            LogMessage = "failed to open github support request",
            DialogRequest = new AppDialogRequest
            {
                Title = strings.SupportUnableOpenTitle,
                Summary = strings.SupportUnableOpenSummary,
                Kind = AppDialogKind.Warning,
                Severity = DialogSeverity.Warning,
                BulletItems =
                [
                    strings.SupportUnableOpenBullet1,
                    strings.SupportUnableOpenBullet2
                ]
            }
        };
    }

    public SupportActionResult OpenLogFolder(string logDirectory, AppStrings strings)
    {
        var directory = (logDirectory ?? "").Trim();
        if (string.IsNullOrWhiteSpace(directory))
        {
            return new SupportActionResult
            {
                IsSuccess = false,
                StatusText = strings.SettingsFailedOpenLogFolder,
                LogCategory = "log",
                LogMessage = "failed to open log folder reason=empty_log_directory"
            };
        }

        LogFolderLaunchResult launchResult;
        try
        {
            launchResult = _logFolderLauncher.Open(directory);
        }
        catch (Exception ex)
        {
            launchResult = new LogFolderLaunchResult
            {
                IsSuccess = false,
                ErrorType = ex.GetType().Name
            };
        }

        if (launchResult.IsSuccess)
        {
            return new SupportActionResult
            {
                IsSuccess = true,
                StatusText = strings.SettingsOpenedLogFolder,
                LogCategory = "app",
                LogMessage = "log folder opened"
            };
        }

        var errorType = (launchResult.ErrorType ?? "").Trim();
        if (string.IsNullOrWhiteSpace(errorType))
        {
            errorType = "UnknownError";
        }

        return new SupportActionResult
        {
            IsSuccess = false,
            StatusText = strings.SettingsFailedOpenLogFolder,
            LogCategory = "log",
            LogMessage = $"failed to open log folder type={errorType}"
        };
    }

    public SupportActionResult OpenGameSupportRequest(ContactIssueContext context, AppLanguage language, AppStrings strings)
    {
        var url = _contactIssueLinkBuilder.BuildIssueUrl(context ?? new ContactIssueContext(), language);
        if (_externalUrlLauncher.OpenUrl(url))
        {
            return new SupportActionResult
            {
                IsSuccess = true,
                StatusText = "",
                LogCategory = "support",
                LogMessage = "github game support request opened"
            };
        }

        return new SupportActionResult
        {
            IsSuccess = false,
            StatusText = strings.SettingsFailedOpenGitHubSupportRequest,
            LogCategory = "support",
            LogMessage = "failed to open github game support request"
        };
    }
}

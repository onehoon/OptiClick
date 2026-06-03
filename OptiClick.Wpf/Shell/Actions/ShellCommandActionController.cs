using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;

namespace OptiClick.Wpf.Shell.Actions;

public sealed class ShellCommandActionController
{
    private readonly StartupNoticePresenter _startupNoticePresenter;
    private readonly SupportIssueContextBuilder _supportIssueContextBuilder;
    private readonly SupportActionController _supportActionController;

    public ShellCommandActionController(
        StartupNoticePresenter startupNoticePresenter,
        SupportIssueContextBuilder supportIssueContextBuilder,
        SupportActionController supportActionController)
    {
        _startupNoticePresenter = startupNoticePresenter ?? throw new ArgumentNullException(nameof(startupNoticePresenter));
        _supportIssueContextBuilder = supportIssueContextBuilder ?? throw new ArgumentNullException(nameof(supportIssueContextBuilder));
        _supportActionController = supportActionController ?? throw new ArgumentNullException(nameof(supportActionController));
    }

    public ShellCommandActionResult BuildAdministratorRelaunchCancelledNotice(AppStrings strings)
    {
        return new ShellCommandActionResult
        {
            SettingsStatusText = strings.StartupAdminCancelledStatus,
            ShouldQueuePendingStartupNotice = true,
            ShouldWriteLog = true,
            LogAsWarning = true,
            LogCategory = "elevation",
            LogMessage = "administrator relaunch canceled_or_failed running_non_elevated"
        };
    }

    public AppDialogRequest BuildAdministratorRelaunchCancelledDialog(AppStrings strings)
    {
        return _startupNoticePresenter.BuildAdministratorRelaunchCancelledDialog(strings);
    }

    public ShellCommandActionResult OpenSupportRequest(
        string appVersion,
        RuntimeContext? runtimeContext,
        AppLanguage language,
        AppStrings strings)
    {
        var context = _supportIssueContextBuilder.Build(appVersion, runtimeContext ?? new RuntimeContext());
        return new ShellCommandActionResult
        {
            SupportActionResult = _supportActionController.OpenSupportRequest(context, language, strings)
        };
    }

    public ShellCommandActionResult OpenGameSupportRequest(
        string appVersion,
        RuntimeContext? runtimeContext,
        AppLanguage language,
        AppStrings strings)
    {
        var context = _supportIssueContextBuilder.Build(appVersion, runtimeContext ?? new RuntimeContext());
        return new ShellCommandActionResult
        {
            SupportActionResult = _supportActionController.OpenGameSupportRequest(context, language, strings)
        };
    }

    public ShellCommandActionResult OpenLogFolder(string logDirectory, AppStrings strings)
    {
        return new ShellCommandActionResult
        {
            SupportActionResult = _supportActionController.OpenLogFolder(logDirectory, strings)
        };
    }
}

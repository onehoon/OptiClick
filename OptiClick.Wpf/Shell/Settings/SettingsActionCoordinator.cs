using System.Diagnostics;
using System.Linq;
using System.Windows;
using OptiClick.Infrastructure.Windows;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Dialogs;

namespace OptiClick.Wpf.Shell.Settings;

public sealed class SettingsActionCoordinator
{
    private readonly DialogPresenter _dialogPresenter;
    private readonly AppCacheResetService _appCacheResetService;
    private readonly IAppLogger _appLogger;

    public SettingsActionCoordinator(
        DialogPresenter dialogPresenter,
        IAppLocalDataPathProvider localDataPathProvider,
        IAppLogger appLogger)
    {
        _dialogPresenter = dialogPresenter ?? throw new ArgumentNullException(nameof(dialogPresenter));
        var safeLocalDataPathProvider = localDataPathProvider ?? throw new ArgumentNullException(nameof(localDataPathProvider));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _appCacheResetService = new AppCacheResetService(safeLocalDataPathProvider, _appLogger);
    }

    public async Task RefreshInstallFilesAsync(
        bool isKoreanUi,
        bool isInstallExecutionInProgress,
        Action<string> applySettingsStatusText,
        CancellationToken cancellationToken = default)
    {
        if (Application.Current is null || isInstallExecutionInProgress)
        {
            return;
        }

        var confirm = await _dialogPresenter.ShowSafelyAsync(
            BuildRefreshInstallFilesConfirmationDialog(isKoreanUi),
            cancellationToken);
        if (confirm != AppDialogResult.Continue)
        {
            return;
        }

        if (!_appCacheResetService.TryReset())
        {
            await _dialogPresenter.ShowSafelyAsync(
                BuildRefreshInstallFilesDeleteFailedDialog(isKoreanUi),
                cancellationToken);
            return;
        }

        var completion = await _dialogPresenter.ShowSafelyAsync(
            BuildRefreshInstallFilesCompletedDialog(isKoreanUi),
            cancellationToken);
        if (completion != AppDialogResult.Ok)
        {
            return;
        }

        if (TryRestartCurrentProcess())
        {
            Application.Current.Shutdown();
            return;
        }

        applySettingsStatusText(isKoreanUi
            ? "\uC571\uC744 \uB2E4\uC2DC \uC2DC\uC791\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."
            : "Failed to restart the app.");
    }

    private static bool TryRestartCurrentProcess()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            var args = Environment.GetCommandLineArgs()
                .Skip(1)
                .Where(arg => !string.Equals(arg, ProcessElevationService.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase))
                .Select(QuoteCommandLineArgument);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
                Arguments = string.Join(" ", args)
            };

            return Process.Start(startInfo) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string QuoteCommandLineArgument(string value)
    {
        var text = value ?? "";
        if (text.Length == 0)
        {
            return "\"\"";
        }

        return text.IndexOfAny([' ', '\t', '"']) >= 0
            ? $"\"{text.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : text;
    }

    private static AppDialogRequest BuildRefreshInstallFilesConfirmationDialog(bool isKoreanUi)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = isKoreanUi ? "\uC571 \uCE90\uC2DC \uCD08\uAE30\uD654" : "Reset app cache",
            Summary = isKoreanUi
                ? "\uC7AC\uC0DD\uC131 \uAC00\uB2A5\uD55C \uC571 \uCE90\uC2DC\uB97C \uC0AD\uC81C\uD569\uB2C8\uB2E4.\n\n\uC124\uCE58 \uC544\uCE74\uC774\uBE0C, \uC124\uCE58 \uC784\uC2DC \uD30C\uC77C, \uCEE4\uBC84 \uC774\uBBF8\uC9C0 \uCE90\uC2DC, \uC9C0\uC6D0 \uAC8C\uC784 \uBAA9\uB85D \uCE90\uC2DC, \uC7A5\uCE58 \uC2DD\uBCC4 \uADDC\uCE59 \uCE90\uC2DC, \uCD5C\uCD08 \uC2E4\uD589 \uC900\uBE44 \uAE30\uB85D\uC744 \uC0AD\uC81C\uD569\uB2C8\uB2E4.\n\n\uC0AC\uC6A9\uC790 \uC124\uC815, \uC2A4\uCE94 \uD3F4\uB354 \uBAA9\uB85D, \uB85C\uADF8, \uAC8C\uC784 \uD3F4\uB354\uC5D0 \uC774\uBBF8 \uC124\uCE58\uB41C \uD30C\uC77C\uC740 \uC720\uC9C0\uB429\uB2C8\uB2E4.\n\uC571\uC744 \uB2E4\uC2DC \uC2DC\uC791\uD558\uBA74 \uD544\uC694\uD55C \uB370\uC774\uD130\uB97C \uC790\uB3D9\uC73C\uB85C \uB2E4\uC2DC \uC900\uBE44\uD569\uB2C8\uB2E4.\n\n\uACC4\uC18D\uD558\uC2DC\uACA0\uC2B5\uB2C8\uAE4C?"
                : "Regenerable app cache will be deleted.\n\nThis includes install archives, install temp files, cover image cache, supported-games cache, device identity rules cache, and the first-run preparation marker.\n\nUser settings, scan folder list, logs, and files already installed in game folders will be kept.\nAfter the app restarts, required data will be prepared automatically again.\n\nDo you want to continue?",
            PrimaryButtonText = isKoreanUi ? "\uCD08\uAE30\uD654" : "Reset",
            SecondaryButtonText = isKoreanUi ? "\uCDE8\uC18C" : "Cancel",
            PrimaryButtonRole = DialogButtonRole.Destructive,
            PrimaryResult = AppDialogResult.Continue,
            SecondaryResult = AppDialogResult.Cancel
        };
    }

    private static AppDialogRequest BuildRefreshInstallFilesCompletedDialog(bool isKoreanUi)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Success,
            Severity = DialogSeverity.Success,
            Title = isKoreanUi ? "\uC571 \uCE90\uC2DC \uCD08\uAE30\uD654" : "Reset app cache",
            Summary = isKoreanUi
                ? "\uC571 \uCE90\uC2DC\uB97C \uCD08\uAE30\uD654\uD588\uC2B5\uB2C8\uB2E4.\n\n\uC571\uC744 \uC0C8\uB85C \uC2DC\uC791\uD574\uC57C \uD569\uB2C8\uB2E4.\n\uD655\uC778\uC744 \uB204\uB974\uBA74 OptiClick\uC774 \uC790\uB3D9\uC73C\uB85C \uB2E4\uC2DC \uC2DC\uC791\uB429\uB2C8\uB2E4."
                : "App cache has been reset.\n\nThe app needs to restart.\nClick OK to restart OptiClick automatically.",
            PrimaryButtonText = isKoreanUi ? "\uD655\uC778" : "OK",
            PrimaryResult = AppDialogResult.Ok
        };
    }

    private static AppDialogRequest BuildRefreshInstallFilesDeleteFailedDialog(bool isKoreanUi)
    {
        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = isKoreanUi ? "\uC571 \uCE90\uC2DC \uCD08\uAE30\uD654" : "Reset app cache",
            Summary = isKoreanUi
                ? "\uC77C\uBD80 \uC571 \uCE90\uC2DC\uB97C \uC0AD\uC81C\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.\n\uC571\uC744 \uC885\uB8CC\uD55C \uB4A4 \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694."
                : "Some app cache files could not be deleted.\nPlease close the app and try again.",
            PrimaryButtonText = isKoreanUi ? "\uD655\uC778" : "OK",
            PrimaryResult = AppDialogResult.Ok
        };
    }
}

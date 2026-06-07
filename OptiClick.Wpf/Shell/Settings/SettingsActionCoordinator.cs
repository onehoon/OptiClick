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
            Title = isKoreanUi ? "설치 파일 다시 다운로드" : "Redownload install files",
            Summary = isKoreanUi
                ? "캐시된 설치 파일을 삭제하고 다시 다운로드할 수 있도록 준비합니다.\n\n삭제 대상: ArchivesV2, ArchivesV2 manifest, 설치 임시 파일, 최초 실행 준비 기록.\n\n커버 이미지 캐시, 지원 게임 목록 캐시, 장치 식별 규칙 캐시, 사용자 설정, 스캔 폴더 목록, 로그, 게임 폴더에 설치된 파일은 유지됩니다.\n앱을 다시 시작하면 필요한 설치 파일을 자동으로 다시 다운로드합니다.\n\n계속하시겠습니까?"
                : "Cached install files will be deleted so they can be downloaded again.\n\nThis includes ArchivesV2 payloads, the ArchivesV2 manifest, install temp files, and the first-run preparation marker.\n\nCover image cache, supported-games cache, device identity rules cache, user settings, scan folder list, logs, and files already installed in game folders will be kept.\nAfter the app restarts, required install files will be downloaded automatically again.\n\nDo you want to continue?",
            PrimaryButtonText = isKoreanUi ? "다시 다운로드" : "Redownload",
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
            Title = isKoreanUi ? "설치 파일 다시 다운로드" : "Redownload install files",
            Summary = isKoreanUi
                ? "캐시된 설치 파일을 삭제했습니다.\n\n앱을 새로 시작해야 합니다.\n확인을 누르면 OptiClick이 자동으로 다시 시작되고 필요한 설치 파일을 다시 다운로드합니다."
                : "Cached install files have been deleted.\n\nThe app needs to restart.\nClick OK to restart OptiClick automatically and redownload the required install files.",
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
            Title = isKoreanUi ? "설치 파일 다시 다운로드" : "Redownload install files",
            Summary = isKoreanUi
                ? "일부 캐시된 설치 파일을 삭제하지 못했습니다.\n앱을 종료한 뒤 다시 시도해주세요."
                : "Some cached install files could not be deleted.\nPlease close the app and try again.",
            PrimaryButtonText = isKoreanUi ? "\uD655\uC778" : "OK",
            PrimaryResult = AppDialogResult.Ok
        };
    }
}

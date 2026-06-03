using System.Globalization;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScanFolderDialogPresenter
{
    public AppDialogRequest BuildPreviewDialog(string folderPath, AppStrings strings)
    {
        return new AppDialogRequest
        {
            Title = strings.ScanFolderPreviewTitle,
            Summary = strings.ScanFolderPreviewSummary,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info,
            BulletItems =
            [
                Format(strings.ScanFolderPreviewSelectedPath, folderPath ?? ""),
                strings.ScanFolderPreviewNoExplorer,
                strings.ScanFolderPreviewNoStart
            ]
        };
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, template ?? "", args ?? []);
    }
}

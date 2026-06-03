using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public sealed class GameDetailsDialogPresenter
{
    public AppDialogRequest BuildDetailsDialog(GameCardViewModel selectedGame, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(selectedGame);
        ArgumentNullException.ThrowIfNull(strings);

        return new AppDialogRequest
        {
            Title = strings.HomeDetails,
            Summary = strings.InstallNoFilesNoNetworkNoSettings,
            Kind = AppDialogKind.Info,
            Severity = DialogSeverity.Info,
            BulletItems = new[]
            {
                $"{strings.HomeSelectGameHint}: {selectedGame.Title}",
                $"Status: {selectedGame.StatusBadge}",
                $"OptiScaler: {selectedGame.OptiScalerSummary}",
                $"Components: {GetComponentSummaryForDisplay(selectedGame.ComponentSummary, strings)}",
                $"Note: {selectedGame.NotePreview}"
            }
        };
    }

    private static string GetComponentSummaryForDisplay(string componentsText, AppStrings strings)
    {
        return string.IsNullOrWhiteSpace(componentsText) ? strings.HomeNoAdditionalComponents : componentsText;
    }
}

using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private void EnsureSupportedGamesWikiLoadedForView()
    {
        SupportedGames.EnsureLoadedForView();
    }

    private void StartSupportedGamesWikiRefreshInBackground()
    {
        SupportedGames.StartRefreshInBackground();
    }

    private void RefreshSupportedGamesAfterLanguageChange()
    {
        SupportedGames.RefreshAfterLanguageChange();
    }

    private void RebuildSupportedGamesWikiRows()
    {
        SupportedGames.RebuildRows();
    }

    public void QueueSupportedGamesWikiVisibleCoverLoad(double verticalOffset, double viewportHeight)
    {
        SupportedGames.QueueVisibleCoverLoad(verticalOffset, viewportHeight);
    }

    private IReadOnlyList<SupportedGamesWikiRowViewModel> ResolveSupportedGamesWikiVisibleRows(
        double verticalOffset,
        double viewportHeight)
    {
        return SupportedGames.ResolveVisibleRows(verticalOffset, viewportHeight);
    }

    internal static string ResolveSupportedGamesWikiCoverSource(
        SupportedGamesWikiEntry entry,
        string defaultCoverSource = CoverImageCacheService.DefaultCoverImageSource)
    {
        return SupportedGamesSectionViewModel.ResolveSupportedGamesWikiCoverSource(entry, defaultCoverSource);
    }

    internal static (string coverSource, string coverLookupUrl, string steamAppId) ResolveSupportedGamesWikiCoverSourceWithLookup(
        SupportedGamesWikiEntry entry,
        string defaultCoverSource = CoverImageCacheService.DefaultCoverImageSource)
    {
        return SupportedGamesSectionViewModel.ResolveSupportedGamesWikiCoverSourceWithLookup(entry, defaultCoverSource);
    }
}

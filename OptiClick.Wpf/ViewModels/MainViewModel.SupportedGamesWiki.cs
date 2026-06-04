using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
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

using System.Windows.Media;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.ViewModels;

public sealed class SupportedGamesWikiRowViewModel : ViewModelBase
{
    private string _coverImageSource = CoverImageCacheService.DefaultCoverImageSource;
    private ImageSource? _coverImage;

    public string GameId { get; init; } = "";
    public string DisplayTitle { get; init; } = "";
    public string GameNameEn { get; init; } = "";
    public string GameNameKr { get; init; } = "";
    public string IntelText { get; init; } = "";
    public string AmdText { get; init; } = "";
    public string NvidiaText { get; init; } = "";
    public string CoverLookupUrl { get; init; } = "";
    public string CoverSteamAppId { get; init; } = "";
    public string CoverImageSource
    {
        get => _coverImageSource;
        set => SetProperty(ref _coverImageSource, value);
    }

    public double CoverImageWidthDip { get; init; } = SupportedGamesWikiLayoutProfile.ResolveWikiRowCoverWidthDip();
    public double CoverImageHeightDip { get; init; } = SupportedGamesWikiLayoutProfile.ResolveWikiRowCoverHeightDip();
    public ImageSource? CoverImage
    {
        get => _coverImage;
        set => SetProperty(ref _coverImage, value);
    }
}

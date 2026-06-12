using OptiClick.Core.Install;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

public interface IRtssOverlayNoticeResolver
{
    string ResolveNotice(ShellGameCardModel game, AppLanguage language);
}

public sealed class RtssOverlayNoticeResolver : IRtssOverlayNoticeResolver
{
    private const string KoreanRtssOverlayNotice = "[RED]선택한 게임에 설치 시 RTSS 오버레이가 Off 됩니다. (충돌 방지)[END]";
    private const string EnglishRtssOverlayNotice = "[RED]RTSS overlay may cause issues with this game.[br]It will be turned off for this game during installation.[END]";

    private readonly IRtssOverlayNoticeStateProvider _noticeStateProvider;

    public RtssOverlayNoticeResolver(IRtssOverlayNoticeStateProvider? noticeStateProvider = null)
    {
        _noticeStateProvider = noticeStateProvider ?? NoopRtssOverlayNoticeStateProvider.Instance;
    }

    public string ResolveNotice(ShellGameCardModel game, AppLanguage language)
    {
        if (game is null || !game.RtssOverlay)
        {
            return "";
        }

        return _noticeStateProvider.IsNoticeRequired()
            ? language == AppLanguage.Korean ? KoreanRtssOverlayNotice : EnglishRtssOverlayNotice
            : "";
    }

}

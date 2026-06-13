using OptiClick.Core.Install;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public interface IShellGameCardViewModelFactory
{
    IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId = null,
        ModuleDownloadLinkContext? moduleDownloadLinks = null);

    IReadOnlyList<GameCardViewModel> CreateCards(
        IReadOnlyList<ShellGameCardModel> games,
        RuntimeContext? runtimeContext,
        IReadOnlyDictionary<string, string>? targetPathByGameId,
        ModuleDownloadLinkContext? moduleDownloadLinks,
        ArchiveReadinessSnapshot? archiveReadiness)
    {
        return CreateCards(games, runtimeContext, targetPathByGameId, moduleDownloadLinks);
    }
}

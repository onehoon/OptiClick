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
}

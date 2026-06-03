using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameExeMatchIndexBuilder
{
    ShellGameExeMatchIndex Build(ShellGameCatalog catalog);
}

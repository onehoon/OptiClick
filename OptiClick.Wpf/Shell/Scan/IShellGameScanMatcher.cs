using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameScanMatcher
{
    IReadOnlyList<ShellGameMatchResult> Match(
        ShellScanResult scanResult,
        ShellGameCatalog catalog,
        RuntimeContext? runtimeContext);
}

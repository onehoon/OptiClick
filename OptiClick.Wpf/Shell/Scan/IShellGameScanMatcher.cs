using OptiClick.Core.Scan;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameScanMatcher
{
    IReadOnlyList<ShellGameMatchResult> Match(
        ExecutableScanResult scanResult,
        ShellGameCatalog catalog,
        RuntimeContext? runtimeContext);
}

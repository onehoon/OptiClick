using OptiClick.Core.Runtime;
using OptiClick.Core.Scan;

namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameScanMatcherWithIndex
{
    IReadOnlyList<ShellGameMatchResult> Match(
        ExecutableScanResult scanResult,
        ShellGameExeMatchIndex matchIndex,
        RuntimeContext? runtimeContext);
}

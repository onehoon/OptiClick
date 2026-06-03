using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Scan;

public interface IShellGameScanMatcherWithIndex
{
    IReadOnlyList<ShellGameMatchResult> Match(
        ShellScanResult scanResult,
        ShellGameExeMatchIndex matchIndex,
        RuntimeContext? runtimeContext);
}

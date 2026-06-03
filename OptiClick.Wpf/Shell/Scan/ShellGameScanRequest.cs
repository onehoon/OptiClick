using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ShellGameScanRequest
{
    public IReadOnlyList<string> ScanFolders { get; init; } = [];
    public ShellGameCatalog Catalog { get; init; } = ShellGameCatalog.Empty;
    public RuntimeContext RuntimeContext { get; init; } = new();
}

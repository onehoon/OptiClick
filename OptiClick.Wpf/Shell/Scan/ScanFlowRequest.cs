using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFlowRequest
{
    public required IReadOnlyList<string> ScanFolders { get; init; }
    public required ShellGameCatalog LatestRemoteCatalog { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required ScanFlowText Text { get; init; }
    public IReadOnlyDictionary<string, ShellGameMatchResult> CurrentMatchByGameId { get; init; }
        = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> CurrentTargetPathByGameId { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFlowRequest
{
    public required IReadOnlyList<string> ScanFolders { get; init; }
    public required ShellGameCatalog LatestRemoteCatalog { get; init; }
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required AppStrings Strings { get; init; }
    public IReadOnlyDictionary<string, ShellGameMatchResult> CurrentMatchByGameId { get; init; }
        = new Dictionary<string, ShellGameMatchResult>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> CurrentTargetPathByGameId { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; }
        = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}

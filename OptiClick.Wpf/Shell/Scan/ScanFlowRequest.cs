using OptiClick.Core.Install;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.RuntimeData;

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
    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; } =
        ArchiveReadinessSnapshot.NotReady;
    public OptiScalerVariantCatalog LatestOptiScalerVariantCatalog { get; init; } =
        OptiScalerVariantCatalog.Empty;
    public string LatestRemoteCatalogErrorCode { get; init; } = "";
}

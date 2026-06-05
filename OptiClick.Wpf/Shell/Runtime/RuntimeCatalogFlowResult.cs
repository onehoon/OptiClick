using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeCatalogFlowResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public bool ShouldApplyRemoteDataState { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public ShellGameCatalog Catalog { get; init; } = ShellGameCatalog.Empty;
    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; init; }
        = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public OptiScalerVariantCatalog OptiScalerVariantCatalog { get; init; } = OptiScalerVariantCatalog.Empty;
    public string SettingsStatusText { get; init; } = "";
    public string ScanStatusText { get; init; } = "";
    public AppDialogRequest? DialogRequest { get; init; }
    public bool ResetRemoteCatalogDialogGate { get; init; }
    public bool ShouldRefreshVisibleGames { get; init; }
    public bool ShouldRefreshArchiveReadiness { get; init; }
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}

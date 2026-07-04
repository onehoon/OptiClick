using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeCatalogFlowResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public bool ShouldApplyRemoteDataState { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public ShellGameCatalog Catalog { get; init; } = ShellGameCatalog.Empty;
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public OptiScalerVariantCatalog OptiScalerVariantCatalog { get; init; } = OptiScalerVariantCatalog.Empty;
    public string GpuBundleKey { get; init; } = "";
    public bool IsAuthV2BusinessStatus { get; init; }
    public string AuthV2Status { get; init; } = "";
    public IReadOnlyList<GpuInfo> AuthV2Candidates { get; init; } = [];
    public string SettingsStatusText { get; init; } = "";
    public string ScanStatusText { get; init; } = "";
    public AppDialogRequest? DialogRequest { get; init; }
    public bool ResetRemoteCatalogDialogGate { get; init; }
    public bool ShouldRefreshVisibleGames { get; init; }
    public bool ShouldRefreshArchiveReadiness { get; init; }
    public IReadOnlyList<RuntimeFlowLogEntry> Logs { get; init; } = [];
}

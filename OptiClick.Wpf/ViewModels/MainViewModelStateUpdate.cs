using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.ViewModels;

public sealed record MainViewModelStateUpdate
{
    public string? SettingsStatusText { get; init; }
    public string? ScanStatusText { get; init; }
    public IReadOnlyList<IFlowLogEntry> FlowLogs { get; init; } = [];
    public string FlowLogFallbackCategory { get; init; } = "";
    public ScanFolderStateUpdate? ScanFolderStateUpdate { get; init; }

    public string? RemoteCatalogErrorCode { get; init; }
    public string? RemoteCatalogDetailErrorCode { get; init; }
    public RemoteRuntimeData? RuntimeData { get; init; }
    public ShellGameCatalog? RemoteCatalog { get; init; }
    public ModuleDownloadLinkContext? ModuleDownloadLinks { get; init; }
    public OptiScalerVariantCatalog? OptiScalerVariantCatalog { get; init; }
    public string? GpuBundleKey { get; init; }

    public bool ShouldResetRemoteCatalogDialogGate { get; init; }
    public bool ShouldRefreshVisibleGames { get; init; }
    public bool ShouldRefreshArchiveReadiness { get; init; }

    public IReadOnlyList<GameCardViewModel>? VisibleGames { get; init; }
    public IReadOnlyDictionary<string, ShellGameMatchResult>? MatchByGameId { get; init; }
    public IReadOnlyDictionary<string, string>? TargetPathByGameId { get; init; }
    public bool ShouldRecomputeSelection { get; init; }
    public bool ShouldNavigateHome { get; init; }

    public AppDialogRequest? DialogRequest { get; init; }
    public PopupPresentationRequest? PopupRequest { get; init; }
    public bool ShouldShutdown { get; init; }
    public bool ShouldQueuePendingStartupNotice { get; init; }

    public bool SupportLogAsWarning { get; init; }
    public string SupportLogCategory { get; init; } = "";
    public string SupportLogMessage { get; init; } = "";
}

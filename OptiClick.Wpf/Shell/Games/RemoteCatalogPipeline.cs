using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Games;

public sealed class RemoteCatalogPipelineResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public bool IsPartial { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteRuntimeData RuntimeData { get; init; } = RemoteRuntimeData.Empty;
    public ShellGameCatalog Catalog { get; init; } = ShellGameCatalog.Empty;
    public RemoteGpuBundleRuntimeLoadResult GpuBundleLoadResult { get; init; } = RemoteGpuBundleRuntimeLoadResult.Skipped();
    public int RuntimeGameCount { get; init; }
    public int BundleGameCount { get; init; }
    public int MatchedGameCount { get; init; }
    public int SupportedGameCount { get; init; }
    public bool IsAuthV2BusinessStatus { get; init; }
    public string AuthV2Status { get; init; } = "";
    public IReadOnlyList<GpuInfo> AuthV2Candidates { get; init; } = [];
}

public interface IRemoteCatalogPipeline
{
    Task<RemoteCatalogPipelineResult> LoadAsync(
        RuntimeContext runtimeContext,
        AppLanguage language,
        CancellationToken cancellationToken = default);
}

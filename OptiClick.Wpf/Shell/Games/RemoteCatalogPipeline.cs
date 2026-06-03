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
}

public interface IRemoteCatalogPipeline
{
    Task<RemoteCatalogPipelineResult> LoadAsync(
        RuntimeContext runtimeContext,
        AppLanguage language,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteCatalogPipeline : IRemoteCatalogPipeline
{
    private readonly IRemoteRuntimeDataLoader _runtimeDataLoader;
    private readonly IRemoteGpuBundleRuntimeLoader _gpuBundleRuntimeLoader;
    private readonly IGpuBundleGameDatabaseMerger _gpuBundleMerger;

    public RemoteCatalogPipeline(
        IRemoteRuntimeDataLoader runtimeDataLoader,
        IRemoteGpuBundleRuntimeLoader gpuBundleRuntimeLoader,
        IGpuBundleGameDatabaseMerger gpuBundleMerger)
    {
        _runtimeDataLoader = runtimeDataLoader ?? throw new ArgumentNullException(nameof(runtimeDataLoader));
        _gpuBundleRuntimeLoader = gpuBundleRuntimeLoader ?? throw new ArgumentNullException(nameof(gpuBundleRuntimeLoader));
        _gpuBundleMerger = gpuBundleMerger ?? throw new ArgumentNullException(nameof(gpuBundleMerger));
    }

    public async Task<RemoteCatalogPipelineResult> LoadAsync(
        RuntimeContext runtimeContext,
        AppLanguage language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var runtimeLoadResult = await _runtimeDataLoader.LoadAsync(cancellationToken);
            if (runtimeLoadResult.IsSkipped)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = true,
                    ErrorCode = "runtime_data_skipped"
                };
            }

            if (!runtimeLoadResult.IsSuccess)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = false,
                    ErrorCode = string.IsNullOrWhiteSpace(runtimeLoadResult.ErrorCode)
                        ? "runtime_data_failed"
                        : runtimeLoadResult.ErrorCode
                };
            }

            var runtimeData = runtimeLoadResult.RuntimeData ?? RemoteRuntimeData.Empty;
            var gpuBundleLoadResult = await _gpuBundleRuntimeLoader.LoadAsync(runtimeContext, cancellationToken);
            if (!gpuBundleLoadResult.IsSuccess)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = false,
                    IsPartial = false,
                    ErrorCode = gpuBundleLoadResult.IsSkipped
                        ? "gpu_bundle_required"
                        : NormalizeErrorCode(gpuBundleLoadResult.ErrorCode, "gpu_bundle_failed"),
                    RuntimeData = runtimeData,
                    GpuBundleLoadResult = gpuBundleLoadResult
                };
            }

            var mergeResult = _gpuBundleMerger.Merge(runtimeData, gpuBundleLoadResult.Bundle);
            if (mergeResult.MatchedGameCount == 0)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = false,
                    IsPartial = false,
                    ErrorCode = "gpu_bundle_merge_no_matches",
                    RuntimeData = runtimeData,
                    GpuBundleLoadResult = gpuBundleLoadResult,
                    RuntimeGameCount = mergeResult.RuntimeGameCount,
                    BundleGameCount = mergeResult.BundleGameCount,
                    MatchedGameCount = mergeResult.MatchedGameCount,
                    SupportedGameCount = mergeResult.SupportedGameCount
                };
            }

            if (mergeResult.SupportedGameCount == 0)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = false,
                    IsPartial = false,
                    ErrorCode = "gpu_bundle_merge_no_supported_games",
                    RuntimeData = runtimeData,
                    GpuBundleLoadResult = gpuBundleLoadResult,
                    RuntimeGameCount = mergeResult.RuntimeGameCount,
                    BundleGameCount = mergeResult.BundleGameCount,
                    MatchedGameCount = mergeResult.MatchedGameCount,
                    SupportedGameCount = mergeResult.SupportedGameCount
                };
            }

            var mapper = new RuntimeDataShellGameMapper(new DictionaryGpuBundleInstallMetadataProvider(mergeResult.MetadataByGameId));
            var catalog = mapper.Map(runtimeData, language);
            if (catalog.Games.Count == 0)
            {
                return new RemoteCatalogPipelineResult
                {
                    IsSuccess = false,
                    IsSkipped = false,
                    IsPartial = false,
                    ErrorCode = "gpu_bundle_merge_no_supported_games",
                    RuntimeData = runtimeData,
                    GpuBundleLoadResult = gpuBundleLoadResult,
                    RuntimeGameCount = mergeResult.RuntimeGameCount,
                    BundleGameCount = mergeResult.BundleGameCount,
                    MatchedGameCount = mergeResult.MatchedGameCount,
                    SupportedGameCount = mergeResult.SupportedGameCount
                };
            }

            return new RemoteCatalogPipelineResult
            {
                IsSuccess = true,
                IsSkipped = false,
                IsPartial = false,
                ErrorCode = "",
                RuntimeData = runtimeData,
                Catalog = catalog,
                GpuBundleLoadResult = gpuBundleLoadResult,
                RuntimeGameCount = mergeResult.RuntimeGameCount,
                BundleGameCount = mergeResult.BundleGameCount,
                MatchedGameCount = mergeResult.MatchedGameCount,
                SupportedGameCount = mergeResult.SupportedGameCount
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RemoteCatalogPipelineResult
            {
                IsSuccess = false,
                IsSkipped = false,
                ErrorCode = "canceled"
            };
        }
        catch
        {
            return new RemoteCatalogPipelineResult
            {
                IsSuccess = false,
                IsSkipped = false,
                ErrorCode = "remote_catalog_unexpected_error"
            };
        }
    }

    private static string NormalizeErrorCode(string value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

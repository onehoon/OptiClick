using OptiClick.Core.Games.GpuBundle;
using OptiClick.Core.Runtime;
using OptiClick.Core.RuntimeData;
using OptiClick.Infrastructure.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.RemoteV2;

public sealed class RemoteCatalogV2Pipeline : IRemoteCatalogPipeline
{
    private readonly IAuthV2SessionClient _authClient;
    private readonly IDataV2Client _dataClient;
    private readonly IRemoteRuntimeDataParser _runtimeDataParser;
    private readonly IRemoteGpuBundleParser _gpuBundleParser;
    private readonly IGpuBundleGameDatabaseMerger _gpuBundleMerger;
    private readonly IAppLogger _logger;

    public RemoteCatalogV2Pipeline(
        IAuthV2SessionClient authClient,
        IDataV2Client dataClient,
        IRemoteRuntimeDataParser runtimeDataParser,
        IRemoteGpuBundleParser gpuBundleParser,
        IGpuBundleGameDatabaseMerger gpuBundleMerger,
        IAppLogger? logger = null)
    {
        _authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));
        _dataClient = dataClient ?? throw new ArgumentNullException(nameof(dataClient));
        _runtimeDataParser = runtimeDataParser ?? throw new ArgumentNullException(nameof(runtimeDataParser));
        _gpuBundleParser = gpuBundleParser ?? throw new ArgumentNullException(nameof(gpuBundleParser));
        _gpuBundleMerger = gpuBundleMerger ?? throw new ArgumentNullException(nameof(gpuBundleMerger));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<RemoteCatalogPipelineResult> LoadAsync(
        RuntimeContext runtimeContext,
        AppLanguage language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var safeContext = runtimeContext ?? new RuntimeContext();
            var appStartId = Guid.NewGuid().ToString("N");
            var authResult = await _authClient.StartSessionAsync(
                safeContext,
                appStartId,
                safeContext.SelectedGpu,
                cancellationToken);
            if (!authResult.IsResolved)
            {
                return Failure(
                    NormalizeErrorCode(authResult.ErrorCode, NormalizeStatusError(authResult.Status)),
                    RemoteRuntimeData.Empty,
                    RemoteGpuBundleRuntimeLoadResult.Failure(NormalizeStatusError(authResult.Status)));
            }

            var runtimeResult = await _dataClient.FetchRuntimeDataAsync(
                authResult.RuntimeToken,
                appStartId,
                cancellationToken);
            if (!runtimeResult.IsSuccess)
            {
                return Failure(
                    NormalizeErrorCode(runtimeResult.ErrorCode, "runtime_data_failed"),
                    RemoteRuntimeData.Empty,
                    RemoteGpuBundleRuntimeLoadResult.Skipped());
            }

            var runtimeParse = _runtimeDataParser.Parse(runtimeResult.RuntimeDataJson);
            if (!runtimeParse.IsSuccess)
            {
                return Failure(
                    NormalizeErrorCode(runtimeParse.ErrorCode, "runtime_data_parse_failed"),
                    RemoteRuntimeData.Empty,
                    RemoteGpuBundleRuntimeLoadResult.Skipped());
            }

            if (string.IsNullOrWhiteSpace(runtimeResult.GpuBundleToken))
            {
                return Failure(
                    "gpu_bundle_token_missing",
                    runtimeParse.RuntimeData,
                    RemoteGpuBundleRuntimeLoadResult.Failure("gpu_bundle_token_missing"));
            }

            var bundleResult = await _dataClient.FetchGpuBundleAsync(
                runtimeResult.GpuBundleToken,
                appStartId,
                cancellationToken);
            if (!bundleResult.IsSuccess)
            {
                return Failure(
                    NormalizeErrorCode(bundleResult.ErrorCode, "gpu_bundle_failed"),
                    runtimeParse.RuntimeData,
                    RemoteGpuBundleRuntimeLoadResult.Failure(NormalizeErrorCode(bundleResult.ErrorCode, "gpu_bundle_failed")));
            }

            var selectedGpu = authResult.ResolvedGpu;
            var bundleParse = _gpuBundleParser.Parse(
                bundleResult.GpuBundleJson,
                selectedGpuGroup: selectedGpu.GpuGroup,
                requestVendor: selectedGpu.Vendor,
                bundleKey: selectedGpu.BundleKey,
                manifestVersion: selectedGpu.ManifestVersion,
                fsr4Policy: Fsr4ManifestPolicy.Disabled);
            if (!bundleParse.IsSuccess)
            {
                return Failure(
                    NormalizeErrorCode(bundleParse.ErrorCode, "gpu_bundle_parse_failed"),
                    runtimeParse.RuntimeData,
                    RemoteGpuBundleRuntimeLoadResult.Failure(NormalizeErrorCode(bundleParse.ErrorCode, "gpu_bundle_parse_failed")));
            }

            var gpuBundleLoadResult = RemoteGpuBundleRuntimeLoadResult.Success(
                bundleParse.Bundle,
                selectedGpu.BundleKey,
                selectedGpu.GpuGroup,
                selectedGpu.Vendor,
                Fsr4ManifestPolicy.Disabled);
            return BuildMergedResult(runtimeParse.RuntimeData, gpuBundleLoadResult, language);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RemoteCatalogPipelineResult
            {
                IsSuccess = false,
                ErrorCode = "remote_v2_canceled"
            };
        }
        catch (Exception ex)
        {
            _logger.Error("remote-v2", "remote v2 catalog pipeline failed", ex);
            return new RemoteCatalogPipelineResult
            {
                IsSuccess = false,
                ErrorCode = "remote_v2_unexpected_error"
            };
        }
    }

    private RemoteCatalogPipelineResult BuildMergedResult(
        RemoteRuntimeData runtimeData,
        RemoteGpuBundleRuntimeLoadResult gpuBundleLoadResult,
        AppLanguage language)
    {
        var mergeResult = _gpuBundleMerger.Merge(runtimeData, gpuBundleLoadResult.Bundle);
        if (mergeResult.MatchedGameCount == 0)
        {
            return new RemoteCatalogPipelineResult
            {
                IsSuccess = false,
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
            RuntimeData = runtimeData,
            Catalog = catalog,
            GpuBundleLoadResult = gpuBundleLoadResult,
            RuntimeGameCount = mergeResult.RuntimeGameCount,
            BundleGameCount = mergeResult.BundleGameCount,
            MatchedGameCount = mergeResult.MatchedGameCount,
            SupportedGameCount = mergeResult.SupportedGameCount
        };
    }

    private static RemoteCatalogPipelineResult Failure(
        string errorCode,
        RemoteRuntimeData runtimeData,
        RemoteGpuBundleRuntimeLoadResult gpuBundleLoadResult)
    {
        return new RemoteCatalogPipelineResult
        {
            IsSuccess = false,
            ErrorCode = NormalizeErrorCode(errorCode, "remote_v2_failed"),
            RuntimeData = runtimeData ?? RemoteRuntimeData.Empty,
            GpuBundleLoadResult = gpuBundleLoadResult ?? RemoteGpuBundleRuntimeLoadResult.Failure("remote_v2_failed")
        };
    }

    private static string NormalizeStatusError(string status)
    {
        var normalized = (status ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "auth_v2_session_not_ready" : normalized;
    }

    private static string NormalizeErrorCode(string value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}


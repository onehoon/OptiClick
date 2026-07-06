using System.Net.Http;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class RemoteGpuBundleRuntimeLoadResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public bool IsUnsupported { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundle Bundle { get; init; } = new();
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string Vendor { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;

    public static RemoteGpuBundleRuntimeLoadResult Success(
        RemoteGpuBundle bundle,
        string bundleKey,
        string gpuGroup,
        string vendor,
        Fsr4ManifestPolicy? fsr4Policy = null)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsSuccess = true,
            Bundle = bundle ?? new RemoteGpuBundle(),
            BundleKey = (bundleKey ?? "").Trim(),
            GpuGroup = (gpuGroup ?? "").Trim().ToLowerInvariant(),
            Vendor = (vendor ?? "").Trim().ToLowerInvariant(),
            Fsr4 = fsr4Policy ?? Fsr4ManifestPolicy.Disabled
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Skipped(string errorCode = "")
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsSkipped = true,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Unsupported(string errorCode)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            IsUnsupported = true,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    public static RemoteGpuBundleRuntimeLoadResult Failure(string errorCode)
    {
        return new RemoteGpuBundleRuntimeLoadResult
        {
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

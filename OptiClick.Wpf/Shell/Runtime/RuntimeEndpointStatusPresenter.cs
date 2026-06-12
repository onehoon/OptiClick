using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeEndpointStatusPresenter
{
    public string BuildStatus(RemoteDataOptions? remoteData, RuntimeEndpointStatusText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var runtimeDataStatus = !string.IsNullOrWhiteSpace(remoteData?.GetEffectiveRuntimeDataUrl())
            ? "configured"
            : "missing";
        var manifestStatus = !string.IsNullOrWhiteSpace(remoteData?.GpuBundleManifestUrl)
            ? "configured"
            : "missing";
        var bundleStatus = !string.IsNullOrWhiteSpace(remoteData?.GpuBundleUrl)
            ? "configured"
            : "missing";
        return string.Format(
            CultureInfo.CurrentCulture,
            text.RuntimeRemoteEndpointsStatusFormat,
            runtimeDataStatus,
            manifestStatus,
            bundleStatus);
    }
}

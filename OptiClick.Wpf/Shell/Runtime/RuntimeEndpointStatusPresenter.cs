using System.Globalization;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeEndpointStatusPresenter
{
    public string BuildStatus(RemoteDataOptions? remoteData, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

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
            strings.RuntimeRemoteEndpointsStatusFormat ?? "",
            runtimeDataStatus,
            manifestStatus,
            bundleStatus);
    }
}

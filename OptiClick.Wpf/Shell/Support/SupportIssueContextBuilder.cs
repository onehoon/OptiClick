using OptiClick.Core.Runtime;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Support;

public sealed class SupportIssueContextBuilder
{
    public ContactIssueContext Build(string appVersion, RuntimeContext? runtimeContext)
    {
        var safeRuntimeContext = runtimeContext ?? new RuntimeContext();
        var selectedGpu = safeRuntimeContext.SelectedGpu;
        var device = safeRuntimeContext.Device;

        return new ContactIssueContext
        {
            AppVersion = appVersion ?? "",
            GpuName = selectedGpu?.Name ?? "",
            Manufacturer = device?.Manufacturer ?? "",
            DeviceModel = device?.Model ?? ""
        };
    }
}

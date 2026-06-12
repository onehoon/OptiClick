using OptiClick.Core.Runtime;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeSummaryStateController
{
    private readonly IDeviceIdentityResolver _deviceIdentityResolver;
    private readonly RuntimeHeaderPresenter _runtimeHeaderPresenter;

    public RuntimeSummaryStateController(
        IDeviceIdentityResolver deviceIdentityResolver,
        RuntimeHeaderPresenter runtimeHeaderPresenter)
    {
        _deviceIdentityResolver = deviceIdentityResolver ?? throw new ArgumentNullException(nameof(deviceIdentityResolver));
        _runtimeHeaderPresenter = runtimeHeaderPresenter ?? throw new ArgumentNullException(nameof(runtimeHeaderPresenter));
    }

    public RuntimeSummaryStateUpdate Build(RuntimeContext? context, RuntimeSummaryStateText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var runtimeContext = context ?? new RuntimeContext();
        var resolvedDevice = _deviceIdentityResolver.Resolve(runtimeContext.Device);
        var selectedGpu = runtimeContext.SelectedGpu;
        var presentation = _runtimeHeaderPresenter.Build(
            resolvedDevice,
            selectedGpu,
            runtimeContext.Gpus,
            text);

        return new RuntimeSummaryStateUpdate
        {
            RuntimeContext = runtimeContext,
            DeviceText = presentation.DeviceText,
            GpuText = presentation.GpuText,
            HasSelectedGpu = selectedGpu is not null,
            SelectedGpuVendor = selectedGpu?.Vendor ?? "",
            SelectedGpuName = selectedGpu?.Name ?? ""
        };
    }
}

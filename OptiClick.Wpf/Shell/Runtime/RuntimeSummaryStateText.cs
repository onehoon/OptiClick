using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeSummaryStateText
{
    public required string RuntimeUnknownDevice { get; init; }
    public required string RuntimeUnknownGpu { get; init; }
    public required string RuntimeGpuSummaryMoreSuffix { get; init; }

    public static RuntimeSummaryStateText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new RuntimeSummaryStateText
        {
            RuntimeUnknownDevice = strings.RuntimeUnknownDevice,
            RuntimeUnknownGpu = strings.RuntimeUnknownGpu,
            RuntimeGpuSummaryMoreSuffix = strings.RuntimeGpuSummaryMoreSuffix
        };
    }
}

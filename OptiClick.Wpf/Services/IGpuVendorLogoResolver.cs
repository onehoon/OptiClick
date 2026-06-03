using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed record GpuVendorLogoPresentation(
    string Source,
    double Width,
    double Height,
    System.Windows.Thickness Margin);

public interface IGpuVendorLogoResolver
{
    string ResolveLogoPath(GpuInfo? selectedGpu);
    GpuVendorLogoPresentation ResolvePresentation(GpuInfo? selectedGpu);
}

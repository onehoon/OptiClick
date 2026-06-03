using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class GpuVendorLogoResolver : IGpuVendorLogoResolver
{
    private const string NvidiaLogo = "pack://application:,,,/Assets/Logos/nvidia.png";
    private const string AmdLogo = "pack://application:,,,/Assets/Logos/amd.png";
    private const string IntelLogo = "pack://application:,,,/Assets/Logos/intel.png";

    public string ResolveLogoPath(GpuInfo? selectedGpu)
    {
        return ResolvePresentation(selectedGpu).Source;
    }

    public GpuVendorLogoPresentation ResolvePresentation(GpuInfo? selectedGpu)
    {
        if (selectedGpu is null)
        {
            return Empty();
        }

        var vendorText = $"{selectedGpu.Vendor} {selectedGpu.Name}".Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(vendorText))
        {
            return Empty();
        }

        if (ContainsAny(vendorText, "nvidia", "geforce", "rtx", "gtx"))
        {
            return new GpuVendorLogoPresentation(
                NvidiaLogo,
                Width: 72,
                Height: 30,
                Margin: new System.Windows.Thickness(0, 0, 8, 0));
        }

        if (ContainsAny(vendorText, "amd", "advanced micro devices", "radeon", "rx ", "780m", "890m"))
        {
            return new GpuVendorLogoPresentation(
                AmdLogo,
                Width: 72,
                Height: 37,
                Margin: new System.Windows.Thickness(0, 0, 8, 0));
        }

        if (ContainsAny(vendorText, "intel", "arc", "iris", "xe"))
        {
            return new GpuVendorLogoPresentation(
                IntelLogo,
                Width: 72,
                Height: 38,
                Margin: new System.Windows.Thickness(0, 0, 8, 0));
        }

        return Empty();
    }

    private static GpuVendorLogoPresentation Empty()
    {
        return new GpuVendorLogoPresentation("", 0, 0, new System.Windows.Thickness(0));
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

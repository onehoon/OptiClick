using OptiClick.Core.Models;

namespace OptiClick.Core.Games.Support;

public sealed class GpuSupportPolicy
{
    private readonly GpuRuleMatcher _ruleMatcher;

    public GpuSupportPolicy(GpuRuleMatcher? ruleMatcher = null)
    {
        _ruleMatcher = ruleMatcher ?? new GpuRuleMatcher();
    }

    public bool IsSupported(GameEntry game, GpuContext gpu)
    {
        if (!game.Enabled || gpu.Vendor == GpuVendor.Unknown)
        {
            return false;
        }

        var vendorFlag = gpu.Vendor switch
        {
            GpuVendor.Intel => game.SupportIntel,
            GpuVendor.Amd => game.SupportAmd,
            GpuVendor.Nvidia => game.SupportNvidia,
            _ => null
        };

        if (vendorFlag.HasValue)
        {
            return vendorFlag.Value;
        }

        if (!string.IsNullOrWhiteSpace(game.SupportedGpu))
        {
            return _ruleMatcher.IsMatch(gpu.RawName, game.SupportedGpu) || _ruleMatcher.IsMatch(gpu.ModelName, game.SupportedGpu);
        }

        return false;
    }
}

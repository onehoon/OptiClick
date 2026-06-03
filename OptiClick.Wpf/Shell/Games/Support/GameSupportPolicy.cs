using OptiClick.Core.Models;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Games.Support;

public sealed class GameSupportPolicy : IGameSupportPolicy
{
    public GameSupportDecision Evaluate(ShellGameCardModel game, RuntimeContext? runtimeContext)
    {
        if (!SupportFlagParser.Parse(game.Enabled, emptyDefault: true, unknownDefault: true, nativeXefgMeansFalse: false))
        {
            return Unsupported(GameSupportReasonCodes.EnabledFalse);
        }

        var vendor = GpuVendorDetector.DetectFromRuntimeContext(runtimeContext);
        var hasVendorSupportFlag = false;

        if (vendor is GpuVendor.Intel or GpuVendor.Amd or GpuVendor.Nvidia)
        {
            var vendorFlag = GetVendorSupportFlag(game, vendor);
            if (vendorFlag.HasValue)
            {
                hasVendorSupportFlag = true;
                if (!vendorFlag.Value)
                {
                    return Unsupported(GameSupportReasonCodes.VendorSupportFalse);
                }
            }
        }

        if (game.GpuBundleLoaded)
        {
            if (game.GpuBundleSupported)
            {
                return Supported(GameSupportReasonCodes.GpuBundleSupported);
            }

            return Unsupported(GameSupportReasonCodes.GpuBundleUnsupported);
        }

        if (hasVendorSupportFlag)
        {
            return Supported(GameSupportReasonCodes.Supported);
        }

        var gpuInfo = ResolveGpuInfoText(runtimeContext);
        if (string.IsNullOrWhiteSpace(gpuInfo))
        {
            return Unsupported(GameSupportReasonCodes.UnknownGpu);
        }

        if (GpuRuleMatcher.IsMatch(game.SupportedGpu, gpuInfo))
        {
            return Supported(GameSupportReasonCodes.SupportedGpuRuleMatched);
        }

        return Unsupported(GameSupportReasonCodes.SupportedGpuRuleMismatch);
    }

    private static bool? GetVendorSupportFlag(ShellGameCardModel game, GpuVendor vendor)
    {
        return vendor switch
        {
            GpuVendor.Intel => game.SupportIntel,
            GpuVendor.Amd => game.SupportAmd,
            GpuVendor.Nvidia => game.SupportNvidia,
            _ => null
        };
    }

    private static string ResolveGpuInfoText(RuntimeContext? runtimeContext)
    {
        var selectedText = (runtimeContext?.SelectedGpu?.Name ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(selectedText))
        {
            return selectedText;
        }

        var gpus = BuildDistinctGpuCandidates(runtimeContext?.Gpus);
        return gpus.Count == 1 ? (gpus[0].Name ?? "").Trim() : "";
    }

    private static IReadOnlyList<GpuInfo> BuildDistinctGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return [];
        }

        var list = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var name = string.Join(" ", (gpu.Name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = string.Join(" ", (gpu.Vendor ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                list.Add(gpu);
            }
        }

        return list;
    }

    private static GameSupportDecision Unsupported(string reasonCode)
    {
        return new GameSupportDecision
        {
            IsSupported = false,
            ReasonCode = reasonCode
        };
    }

    private static GameSupportDecision Supported(string reasonCode)
    {
        return new GameSupportDecision
        {
            IsSupported = true,
            ReasonCode = reasonCode
        };
    }
}

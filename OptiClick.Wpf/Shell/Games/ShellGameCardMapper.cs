using OptiClick.Core.Models;
using OptiClick.Infrastructure.Install.Components;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public static class ShellGameCardMapper
{
    private static readonly IFsr4InstallEligibilityResolver Fsr4EligibilityResolver = new Fsr4InstallEligibilityResolver();

    public static ShellGameCardModel Map(GameCardViewModel card)
    {
        if (card.SourceModel is not null)
        {
            return card.SourceModel;
        }

        var entry = card.GameEntry;
        return new ShellGameCardModel
        {
            GameId = (entry.GameId ?? "").Trim(),
            MatchExe = entry.MatchFiles.FirstOrDefault() ?? "",
            DisplayName = (card.Title ?? "").Trim(),
            GameNameEn = (entry.GameNameEn ?? "").Trim(),
            GameNameKr = (entry.GameNameKr ?? "").Trim(),
            Enabled = entry.Enabled,
            SupportIntel = entry.SupportIntel,
            SupportAmd = entry.SupportAmd,
            SupportNvidia = entry.SupportNvidia,
            SupportedGpu = (entry.SupportedGpu ?? "").Trim(),
            OptiScalerDllName = (entry.OptiScalerDllName ?? "").Trim(),
            ReframeworkUrl = (entry.ReframeworkUrl ?? "").Trim(),
            SpecialK = (entry.SpecialK ?? "").Trim(),
            UltimateAsiLoader = entry.UltimateAsiLoader,
            OptiPatcher = entry.OptiPatcher,
            Unreal5 = entry.Unreal5,
            RtssOverlay = entry.RtssOverlay,
            ExtraBundle = (entry.ExtraBundle ?? "").Trim(),
            InstallMetadata = new MergedGameInstallMetadata
            {
                OptiScalerDllName = (entry.OptiScalerDllName ?? "").Trim(),
                ReFrameworkUrl = (entry.ReframeworkUrl ?? "").Trim(),
                SpecialK = (entry.SpecialK ?? "").Trim(),
                ExtraBundle = (entry.ExtraBundle ?? "").Trim(),
                UltimateAsiLoader = entry.UltimateAsiLoader,
                OptiPatcher = entry.OptiPatcher,
                Unreal5 = entry.Unreal5,
                RtssOverlay = entry.RtssOverlay
            }
        };
    }

    public static bool ResolveFsr4Required(ShellGameCardModel? selectedGame)
    {
        if (selectedGame is null)
        {
            return false;
        }

        var eligibility = Fsr4EligibilityResolver.Resolve(new Fsr4InstallEligibilityContext
        {
            // FSR4 is enabled for every game unless the current GPU is excluded.
            UseFsr4 = true,
            GpuGroup = ShellGameInstallMetadataResolver.GetGpuGroup(selectedGame),
            GpuBundleKey = ShellGameInstallMetadataResolver.GetGpuBundleKey(selectedGame)
        });
        return eligibility.CanInstall;
    }
}

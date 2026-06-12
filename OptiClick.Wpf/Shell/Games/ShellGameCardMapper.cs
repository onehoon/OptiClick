using OptiClick.Core.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Games;

public static class ShellGameCardMapper
{
    public static ShellGameCardModel Map(GameCardViewModel? card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.SourceModel is not null)
        {
            return card.SourceModel;
        }

        var entry = card.GameEntry;
        // Keep InstallMetadata populated for descriptor pipeline canonicalization,
        // while card-level install fields remain for compatibility and display consumers.
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
}

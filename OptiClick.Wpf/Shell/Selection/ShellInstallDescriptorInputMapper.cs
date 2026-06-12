using OptiClick.Core.Install;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

internal static class ShellInstallDescriptorInputMapper
{
    public static InstallDescriptorInput FromShellGame(ShellGameCardModel? game, MergedGameInstallMetadata? metadata)
    {
        if (game is null)
        {
            return InstallDescriptorInput.Empty;
        }

        var mergedMetadata = metadata ?? MergedGameInstallMetadata.Empty;
        return new InstallDescriptorInput
        {
            GameId = (game.GameId ?? "").Trim(),
            DisplayName = (game.DisplayName ?? "").Trim(),
            GameNameEn = (game.GameNameEn ?? "").Trim(),
            GameNameKr = (game.GameNameKr ?? "").Trim(),
            MatchExe = (game.MatchExe ?? "").Trim(),
            OptiScalerDllName = mergedMetadata.OptiScalerDllName,
            ReFrameworkUrl = mergedMetadata.ReFrameworkUrl,
            SpecialK = mergedMetadata.SpecialK,
            RequiresUltimateAsiLoader = mergedMetadata.UltimateAsiLoader,
            RequiresOptiPatcher = mergedMetadata.OptiPatcher,
            RequiresUnreal5 = mergedMetadata.Unreal5,
            RequiresRtssProfile = mergedMetadata.RtssOverlay,
            ExtraBundle = mergedMetadata.ExtraBundle,
            ExcludeListRaw = mergedMetadata.ExcludeListRaw,
            ExcludeListPatterns = mergedMetadata.ExcludeListPatterns,
            GpuBundleKey = mergedMetadata.GpuBundleKey,
            GpuGroup = mergedMetadata.GpuGroup,
            Fsr4 = mergedMetadata.Fsr4 ?? Fsr4ManifestPolicy.Disabled,
            IniSettings = mergedMetadata.IniSettings,
            GameIniProfileRows = mergedMetadata.GameIniProfileRows,
            GameUnrealIniProfileRows = mergedMetadata.GameUnrealIniProfileRows,
            EngineIniProfileRows = mergedMetadata.EngineIniProfileRows,
            GameXmlProfileRows = mergedMetadata.GameXmlProfileRows,
            RegistryProfileRows = mergedMetadata.RegistryProfileRows,
            GameJsonProfileRows = mergedMetadata.GameJsonProfileRows,
            IsEnabled = game.Enabled
        };
    }
}

using OptiClick.Core.OptiScaler;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Core.Install;

public static class ResolvedInstallGameInputsMapper
{
    public static ResolvedInstallGameInputs FromInput(InstallDescriptorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var profileRows = BuildProfileRows(input);
        return new ResolvedInstallGameInputs
        {
            ExecutionDescriptor = InstallExecutionDescriptorMapper.FromInput(input),
            ProfileRows = profileRows,
            OptiScalerIniApplyContext = new OptiScalerIniApplyContext
            {
                GameOptiScalerIniSettings = input.IniSettings
            },
            EngineIniProfileRows = profileRows.EngineIniProfileRows,
            IsEnabled = input.IsEnabled
        };
    }

    private static AttachedRuntimeProfileRows BuildProfileRows(InstallDescriptorInput input)
    {
        return new AttachedRuntimeProfileRows
        {
            GameIniProfileRows = input.GameIniProfileRows,
            GameUnrealIniProfileRows = input.GameUnrealIniProfileRows,
            EngineIniProfileRows = input.EngineIniProfileRows,
            GameXmlProfileRows = input.GameXmlProfileRows,
            RegistryProfileRows = input.RegistryProfileRows,
            GameJsonProfileRows = input.GameJsonProfileRows
        };
    }
}

public static class InstallExecutionDescriptorMapper
{
    public static InstallExecutionDescriptor FromInput(InstallDescriptorInput? input)
    {
        if (input is null)
        {
            return InstallExecutionDescriptor.Empty;
        }

        var shouldInstallFsr4 = InstallFsr4RequirementResolver.Resolve(input.Fsr4.Enabled, input.Fsr4.Variant);
        return new InstallExecutionDescriptor
        {
            GameDescriptor = InstallGameDescriptorMapper.FromInput(input) ?? InstallGameDescriptor.Empty,
            GpuBundleKey = input.GpuBundleKey,
            GpuGroup = input.GpuGroup,
            ShouldInstallFsr4 = shouldInstallFsr4,
            Fsr4Variant = shouldInstallFsr4 ? CoreFsr4InstallPolicy.NormalizeVariant(input.Fsr4.Variant) : ""
        };
    }
}

public static class InstallGameDescriptorMapper
{
    public static InstallGameDescriptor? FromInput(InstallDescriptorInput? input)
    {
        if (input is null)
        {
            return null;
        }

        return new InstallGameDescriptor
        {
            GameId = input.GameId,
            DisplayName = input.DisplayName,
            GameNameEn = input.GameNameEn,
            GameNameKr = input.GameNameKr,
            MatchExe = input.MatchExe,
            OptiScalerDllName = input.OptiScalerDllName,
            ReFrameworkUrl = input.ReFrameworkUrl,
            SpecialK = input.SpecialK,
            RequiresOptiPatcher = input.RequiresOptiPatcher,
            RequiresUnreal5 = input.RequiresUnreal5,
            RequiresRtssProfile = input.RequiresRtssProfile,
            ExtraBundle = input.ExtraBundle,
            ExcludeListRaw = input.ExcludeListRaw,
            ExcludeListPatterns = input.ExcludeListPatterns
        };
    }
}

public static class InstallFsr4RequirementResolver
{
    private static readonly CoreFsr4InstallPolicy InstallPolicy = new();

    public static bool Resolve(bool manifestEnabled, string variant)
    {
        return InstallPolicy.ShouldInstall(manifestEnabled, variant);
    }
}

using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallComponentPolicy
{
    private sealed record OptionalComponentRule(
        CoreInstallPlanComponentType Type,
        Func<InstallGameDescriptor, CoreInstallPlanBuildInput, bool> IsEnabled,
        string SourceKind,
        Func<InstallGameDescriptor, string> DestinationHintFactory,
        Func<InstallGameDescriptor, CoreInstallPlanBuildInput, string> RequiredAliasFactory,
        string SkipReason);

    private static readonly IReadOnlyList<OptionalComponentRule> OptionalComponentRules =
    [
        new(
            CoreInstallPlanComponentType.REFramework,
            static (game, _) => !string.IsNullOrWhiteSpace(game.ReFrameworkUrl),
            "archive",
            static game =>
            {
                var value = game.ReFrameworkUrl;
                return string.IsNullOrWhiteSpace(value) ? "dinput8.dll" : value;
            },
            static (_, _) => "reframework",
            CoreInstallComponentReasonCodes.ReFrameworkNotRequested),
        new(
            CoreInstallPlanComponentType.SpecialK,
            static (game, _) => !string.IsNullOrWhiteSpace(game.SpecialK),
            "archive",
            static game =>
            {
                var value = game.SpecialK;
                return string.IsNullOrWhiteSpace(value) ? OptiScalerInstallLayout.PluginsToken : value;
            },
            static (_, _) => "specialk",
            CoreInstallComponentReasonCodes.SpecialKNotRequested),
        new(
            CoreInstallPlanComponentType.Unreal5,
            static (game, _) => game.RequiresUnreal5,
            "archive",
            static _ => "game_root",
            static (_, _) => "unreal5",
            CoreInstallComponentReasonCodes.Unreal5NotRequested),
        new(
            CoreInstallPlanComponentType.ExtraBundle,
            static (game, _) => !string.IsNullOrWhiteSpace(game.ExtraBundle),
            "archive",
            static _ => "game_root",
            static (game, _) => game.ExtraBundle,
            CoreInstallComponentReasonCodes.ExtraBundleNotRequested),
        new(
            CoreInstallPlanComponentType.Fsr4,
            static (_, input) => input.ShouldInstallFsr4,
            "archive",
            static _ => "game_root",
            static (_, input) => CoreFsr4VariantArchiveKeys.ToArchiveAlias(input.Fsr4Variant),
            CoreInstallComponentReasonCodes.Fsr4SkippedByGpuPolicy),
        new(
            CoreInstallPlanComponentType.RtssProfile,
            static (game, _) => game.RequiresRtssProfile,
            "profile",
            static _ => "rtss_profile",
            static (_, _) => "rtss",
            CoreInstallComponentReasonCodes.RtssNotRequested)
    ];

    public IReadOnlyList<CoreInstallPlanComponent> ResolveComponents(CoreInstallPlanBuildInput input)
    {
        if (input is null)
        {
            return Array.Empty<CoreInstallPlanComponent>();
        }

        var game = input.GameDescriptor;
        if (game is null)
        {
            return Array.Empty<CoreInstallPlanComponent>();
        }

        var components = new List<CoreInstallPlanComponent>
        {
            Component(CoreInstallPlanComponentType.OptiScalerCore, enabled: true, sourceKind: "archive", destinationHint: "game_root", requiredAlias: "optiscaler")
        };

        foreach (var rule in OptionalComponentRules)
        {
            components.Add(OptionalComponent(
                rule.Type,
                rule.IsEnabled(game, input),
                rule.SourceKind,
                rule.DestinationHintFactory(game),
                rule.RequiredAliasFactory(game, input),
                rule.SkipReason));
        }

        return components;
    }

    private static CoreInstallPlanComponent Component(
        CoreInstallPlanComponentType type,
        bool enabled,
        string sourceKind,
        string destinationHint,
        string requiredAlias)
    {
        return new CoreInstallPlanComponent
        {
            Type = type,
            Enabled = enabled,
            SourceKind = sourceKind,
            DestinationHint = destinationHint,
            RequiredArchiveAlias = (requiredAlias ?? "").Trim()
        };
    }

    private static CoreInstallPlanComponent OptionalComponent(
        CoreInstallPlanComponentType type,
        bool enabled,
        string sourceKind,
        string destinationHint,
        string requiredAlias,
        string skipReason)
    {
        return new CoreInstallPlanComponent
        {
            Type = type,
            Enabled = enabled,
            SkipReason = enabled ? "" : skipReason,
            SourceKind = sourceKind,
            DestinationHint = destinationHint,
            RequiredArchiveAlias = (requiredAlias ?? "").Trim()
        };
    }
}

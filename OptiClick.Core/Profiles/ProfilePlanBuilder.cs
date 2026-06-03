using OptiClick.Core.Models;

namespace OptiClick.Core.Profiles;

public sealed class ProfilePlanBuilder
{
    public IReadOnlyList<ProfilePlan> BuildProfilePlan(GameEntry game, IReadOnlyList<ProfilePlan> profileCatalog)
    {
        var plans = new List<ProfilePlan>();
        var order = 0;

        if (game.IniSettings.Count > 0)
        {
            plans.Add(new ProfilePlan
            {
                ProfileKind = "optiscaler_ini",
                ProfileId = "inline_ini_settings",
                Order = order++,
                Target = "OptiScaler.ini"
            });
        }

        foreach (var kind in ProfileApplicationOrder.Kinds.Where(kind => kind != "optiscaler_ini" && kind != "rtss"))
        {
            foreach (var profile in profileCatalog.Where(profile => profile.ProfileKind.Equals(kind, StringComparison.OrdinalIgnoreCase)))
            {
                plans.Add(profile with { Order = order++ });
            }
        }

        return plans;
    }

    public IReadOnlyList<RegistryPlan> BuildRegistryPlan(IReadOnlyList<ProfilePlan> profileCatalog)
    {
        return Array.Empty<RegistryPlan>();
    }

    public IReadOnlyList<RtssActionPlan> BuildRtssPlan(GameEntry game)
    {
        if (!game.RtssOverlay)
        {
            return Array.Empty<RtssActionPlan>();
        }

        return new[]
        {
            new RtssActionPlan
            {
                Action = "plan_rtss_overlay_profile",
                Target = game.GameId,
                Required = true,
                Reason = "rtss_overlay"
            }
        };
    }
}

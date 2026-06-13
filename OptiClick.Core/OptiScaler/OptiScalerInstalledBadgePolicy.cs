namespace OptiClick.Core.OptiScaler;

public enum OptiScalerInstalledBadgeCode
{
    NotInstalled,
    UpdateAvailable,
    LatestStable,
    PreviewInstalled,
    InstalledVersion
}

public sealed record OptiScalerInstalledBadgeDecision
{
    public OptiScalerInstalledBadgeCode Code { get; init; } = OptiScalerInstalledBadgeCode.NotInstalled;
    public string DisplayVersion { get; init; } = "";
}

public static class OptiScalerInstalledBadgePolicy
{
    public static OptiScalerInstalledBadgeDecision Evaluate(
        OptiScalerVersionIdentity? installed,
        OptiScalerVersionIdentity? target,
        OptiScalerVersionUpdateDecision? updateDecision)
    {
        var installedIdentity = installed ?? new OptiScalerVersionIdentity();
        if (!HasInstalledIdentity(installedIdentity))
        {
            return new OptiScalerInstalledBadgeDecision
            {
                Code = OptiScalerInstalledBadgeCode.NotInstalled
            };
        }

        var safeUpdateDecision = updateDecision
                                 ?? OptiScalerVersionUpdatePolicy.Evaluate(installedIdentity, target);
        if (safeUpdateDecision.Code == OptiScalerVersionUpdateCode.UpdateAvailable)
        {
            return new OptiScalerInstalledBadgeDecision
            {
                Code = OptiScalerInstalledBadgeCode.UpdateAvailable,
                DisplayVersion = safeUpdateDecision.TargetDisplayVersion
            };
        }

        var installedDisplayVersion = safeUpdateDecision.InstalledDisplayVersion;
        var installedChannel = OptiScalerVersionUpdatePolicy.ResolveChannel(installedIdentity);
        if (installedChannel == OptiScalerVersionChannel.Preview)
        {
            return new OptiScalerInstalledBadgeDecision
            {
                Code = OptiScalerInstalledBadgeCode.PreviewInstalled,
                DisplayVersion = installedDisplayVersion
            };
        }

        if (installedChannel == OptiScalerVersionChannel.Stable
            && safeUpdateDecision.Code == OptiScalerVersionUpdateCode.Latest)
        {
            return new OptiScalerInstalledBadgeDecision
            {
                Code = OptiScalerInstalledBadgeCode.LatestStable
            };
        }

        return new OptiScalerInstalledBadgeDecision
        {
            Code = OptiScalerInstalledBadgeCode.InstalledVersion,
            DisplayVersion = installedDisplayVersion
        };
    }

    private static bool HasInstalledIdentity(OptiScalerVersionIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.FileVersion)
               || !string.IsNullOrWhiteSpace(identity.ProductVersion)
               || !string.IsNullOrWhiteSpace(identity.DisplayVersion);
    }
}

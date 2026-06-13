namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerVersionTargetSet
{
    public OptiScalerVersionIdentity Selected { get; init; } = new();
    public OptiScalerVersionIdentity Stable { get; init; } = new();
    public OptiScalerVersionIdentity Preview { get; init; } = new();
}

public static class OptiScalerVersionTargetPolicy
{
    public static OptiScalerVersionIdentity ResolveTargetForInstalled(
        OptiScalerVersionIdentity? installed,
        OptiScalerVersionTargetSet? targets)
    {
        var installedIdentity = installed ?? new OptiScalerVersionIdentity();
        var targetSet = targets ?? new OptiScalerVersionTargetSet();
        var selected = targetSet.Selected ?? new OptiScalerVersionIdentity();
        var stable = targetSet.Stable ?? new OptiScalerVersionIdentity();
        var preview = targetSet.Preview ?? new OptiScalerVersionIdentity();

        return OptiScalerVersionUpdatePolicy.ResolveChannel(installedIdentity) switch
        {
            OptiScalerVersionChannel.Preview => PickFirstIdentity(preview, stable, selected, installedIdentity),
            OptiScalerVersionChannel.Stable => PickFirstIdentity(stable, selected, installedIdentity),
            _ => PickFirstIdentity(selected, stable, preview, installedIdentity)
        };
    }

    public static bool HasVersionIdentity(OptiScalerVersionIdentity? identity)
    {
        if (identity is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(identity.FileVersion)
               || !string.IsNullOrWhiteSpace(identity.ProductVersion)
               || !string.IsNullOrWhiteSpace(identity.DisplayVersion);
    }

    private static OptiScalerVersionIdentity PickFirstIdentity(params OptiScalerVersionIdentity[] identities)
    {
        foreach (var identity in identities)
        {
            if (HasVersionIdentity(identity))
            {
                return identity;
            }
        }

        return new OptiScalerVersionIdentity();
    }
}

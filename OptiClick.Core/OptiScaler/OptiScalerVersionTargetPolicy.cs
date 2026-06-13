namespace OptiClick.Core.OptiScaler;

public sealed record OptiScalerVersionTargetSet
{
    public string PreferredVariant { get; init; } = "";
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
        var preferredVariant = ResolvePreferredVariant(targetSet.PreferredVariant, selected);

        return preferredVariant switch
        {
            OptiScalerVariantPreference.PreviewVariant => ResolvePreviewTarget(selected, stable, preview, installedIdentity),
            OptiScalerVariantPreference.StableVariant => ResolveStableTarget(selected, stable, installedIdentity),
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

    private static OptiScalerVersionIdentity ResolveStableTarget(
        OptiScalerVersionIdentity selected,
        OptiScalerVersionIdentity stable,
        OptiScalerVersionIdentity installed)
    {
        if (HasVersionIdentity(stable))
        {
            return stable;
        }

        return OptiScalerVersionUpdatePolicy.ResolveChannel(selected) == OptiScalerVersionChannel.Preview
            ? installed
            : PickFirstIdentity(selected, installed);
    }

    private static OptiScalerVersionIdentity ResolvePreviewTarget(
        OptiScalerVersionIdentity selected,
        OptiScalerVersionIdentity stable,
        OptiScalerVersionIdentity preview,
        OptiScalerVersionIdentity installed)
    {
        if (HasVersionIdentity(preview))
        {
            return preview;
        }

        return PickFirstIdentity(stable, selected, installed);
    }

    private static string ResolvePreferredVariant(string preferredVariant, OptiScalerVersionIdentity selected)
    {
        var normalized = OptiScalerVariantPreference.Normalize(preferredVariant);
        if (string.Equals(normalized, OptiScalerVariantPreference.StableVariant, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, OptiScalerVariantPreference.PreviewVariant, StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return OptiScalerVersionUpdatePolicy.ResolveChannel(selected) switch
        {
            OptiScalerVersionChannel.Preview => OptiScalerVariantPreference.PreviewVariant,
            OptiScalerVersionChannel.Stable => OptiScalerVariantPreference.StableVariant,
            _ => ""
        };
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

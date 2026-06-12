namespace OptiClick.Core.OptiScaler;

public static class OptiScalerVariantPreference
{
    public const string StableVariant = "stable";
    public const string PreviewVariant = "preview";

    public static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static string NormalizeOrDefault(string? value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            StableVariant => StableVariant,
            PreviewVariant => PreviewVariant,
            _ => StableVariant
        };
    }
}

namespace OptiClick.Core.Install;

public static class CoreFsr4VariantArchiveKeys
{
    public const string ResourceKey = "fsr4_variants";

    public static string ToArchiveAlias(string variant)
    {
        var normalized = NormalizeVariant(variant);
        return string.IsNullOrWhiteSpace(normalized)
            ? ResourceKey
            : $"{ResourceKey}:{normalized}";
    }

    public static string NormalizeVariant(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }
}

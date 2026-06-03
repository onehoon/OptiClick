namespace OptiClick.Infrastructure.Install.Components;

public interface IFsr4InstallEligibilityResolver
{
    Fsr4InstallEligibility Resolve(Fsr4InstallEligibilityContext context);
}

public sealed class Fsr4InstallEligibilityResolver : IFsr4InstallEligibilityResolver
{
    private static readonly string[] DefaultExcludedGpuGroups = ["radeon_rx90"];
    private static readonly string[] DefaultExcludedBundleKeys = ["radeon_rx90"];

    private readonly HashSet<string> _excludedGpuGroups;
    private readonly HashSet<string> _excludedBundleKeys;

    public Fsr4InstallEligibilityResolver(
        IEnumerable<string>? excludedGpuGroups = null,
        IEnumerable<string>? excludedBundleKeys = null)
    {
        _excludedGpuGroups = CreateNormalizedSet(excludedGpuGroups ?? DefaultExcludedGpuGroups);
        _excludedBundleKeys = CreateNormalizedSet(excludedBundleKeys ?? DefaultExcludedBundleKeys);
    }

    public Fsr4InstallEligibility Resolve(Fsr4InstallEligibilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (IsExcluded(context.GpuGroup, _excludedGpuGroups)
            || IsExcluded(context.GpuBundleKey, _excludedBundleKeys))
        {
            return new Fsr4InstallEligibility
            {
                CanInstall = false,
                SkipReason = "gpu_excluded"
            };
        }

        if (!context.UseFsr4)
        {
            return new Fsr4InstallEligibility
            {
                CanInstall = false,
                SkipReason = "not_required"
            };
        }

        return new Fsr4InstallEligibility
        {
            CanInstall = true
        };
    }

    private static HashSet<string> CreateNormalizedSet(IEnumerable<string> values)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawValue in values)
        {
            var normalized = Normalize(rawValue);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            set.Add(normalized);
        }

        return set;
    }

    private static bool IsExcluded(string value, HashSet<string> excluded)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return excluded.Contains(normalized);
    }

    private static string Normalize(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }
}

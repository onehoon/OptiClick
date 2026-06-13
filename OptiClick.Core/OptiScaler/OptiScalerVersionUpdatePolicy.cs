using System.Text.RegularExpressions;

namespace OptiClick.Core.OptiScaler;

public enum OptiScalerVersionChannel
{
    Unknown,
    Stable,
    Preview
}

public enum OptiScalerVersionUpdateCode
{
    Latest,
    UpdateAvailable,
    PreRelease
}

public sealed record OptiScalerVersionIdentity
{
    public string Variant { get; init; } = "";
    public string FileVersion { get; init; } = "";
    public string ProductVersion { get; init; } = "";
    public string DisplayVersion { get; init; } = "";

    public OptiScalerVersionChannel Channel => OptiScalerVersionUpdatePolicy.ResolveChannel(this);

    public string ResolveDisplayVersion()
    {
        return OptiScalerVersionUpdatePolicy.ResolveDisplayVersion(this);
    }
}

public sealed record OptiScalerVersionUpdateDecision
{
    public OptiScalerVersionUpdateCode Code { get; init; }
    public string InstalledDisplayVersion { get; init; } = "";
    public string TargetDisplayVersion { get; init; } = "";
}

public static class OptiScalerVersionUpdatePolicy
{
    private static readonly Regex VersionTokenRegex = new(@"\d+(?:[\.,]\d+)*", RegexOptions.Compiled);

    public static OptiScalerVersionUpdateDecision Evaluate(
        OptiScalerVersionIdentity? installed,
        OptiScalerVersionIdentity? target)
    {
        var installedIdentity = installed ?? new OptiScalerVersionIdentity();
        var targetIdentity = target ?? new OptiScalerVersionIdentity();
        var installedDisplayVersion = ResolveDisplayVersion(installedIdentity);
        var targetDisplayVersion = ResolveDisplayVersion(targetIdentity);
        var installedChannel = ResolveChannel(installedIdentity);
        var targetChannel = ResolveChannel(targetIdentity);
        var comparison = CompareVersionTuples(
            ResolveNumericVersionCandidate(installedIdentity),
            ResolveNumericVersionCandidate(targetIdentity));

        if (!HasTargetIdentity(targetIdentity))
        {
            return Decision(OptiScalerVersionUpdateCode.UpdateAvailable, installedDisplayVersion, targetDisplayVersion);
        }

        if (installedChannel == OptiScalerVersionChannel.Preview
            && targetChannel == OptiScalerVersionChannel.Stable)
        {
            return Decision(
                comparison is not null && comparison > 0
                    ? OptiScalerVersionUpdateCode.PreRelease
                    : OptiScalerVersionUpdateCode.UpdateAvailable,
                installedDisplayVersion,
                targetDisplayVersion);
        }

        if (installedChannel == OptiScalerVersionChannel.Stable
            && targetChannel == OptiScalerVersionChannel.Preview)
        {
            return Decision(
                comparison switch
                {
                    < 0 => OptiScalerVersionUpdateCode.UpdateAvailable,
                    > 0 => OptiScalerVersionUpdateCode.PreRelease,
                    _ => OptiScalerVersionUpdateCode.Latest
                },
                installedDisplayVersion,
                targetDisplayVersion);
        }

        if (comparison is null)
        {
            return Decision(
                HasSameProductIdentity(installedIdentity, targetIdentity)
                    ? OptiScalerVersionUpdateCode.Latest
                    : OptiScalerVersionUpdateCode.UpdateAvailable,
                installedDisplayVersion,
                targetDisplayVersion);
        }

        if (comparison < 0)
        {
            return Decision(OptiScalerVersionUpdateCode.UpdateAvailable, installedDisplayVersion, targetDisplayVersion);
        }

        if (comparison > 0)
        {
            return Decision(OptiScalerVersionUpdateCode.PreRelease, installedDisplayVersion, targetDisplayVersion);
        }

        if (installedChannel == OptiScalerVersionChannel.Preview
            && targetChannel == OptiScalerVersionChannel.Preview
            && HasProductIdentity(installedIdentity)
            && HasProductIdentity(targetIdentity)
            && !HasSameProductIdentity(installedIdentity, targetIdentity))
        {
            return Decision(OptiScalerVersionUpdateCode.UpdateAvailable, installedDisplayVersion, targetDisplayVersion);
        }

        return Decision(OptiScalerVersionUpdateCode.Latest, installedDisplayVersion, targetDisplayVersion);
    }

    public static OptiScalerVersionChannel ResolveChannel(OptiScalerVersionIdentity identity)
    {
        var variant = Normalize(identity.Variant);
        if (string.Equals(variant, OptiScalerVariantPreference.PreviewVariant, StringComparison.OrdinalIgnoreCase))
        {
            return OptiScalerVersionChannel.Preview;
        }

        if (string.Equals(variant, OptiScalerVariantPreference.StableVariant, StringComparison.OrdinalIgnoreCase))
        {
            return OptiScalerVersionChannel.Stable;
        }

        var productVersion = Normalize(identity.ProductVersion);
        var displayVersion = Normalize(identity.DisplayVersion);
        if (ContainsPreviewMarker(productVersion) || ContainsPreviewMarker(displayVersion))
        {
            return OptiScalerVersionChannel.Preview;
        }

        if (ContainsFinalMarker(productVersion) || ContainsFinalMarker(displayVersion))
        {
            return OptiScalerVersionChannel.Stable;
        }

        return OptiScalerVersionChannel.Unknown;
    }

    public static string ResolveDisplayVersion(OptiScalerVersionIdentity identity)
    {
        var displayVersion = Normalize(identity.DisplayVersion);
        if (!string.IsNullOrWhiteSpace(displayVersion))
        {
            return displayVersion;
        }

        var productVersion = Normalize(identity.ProductVersion);
        if (!string.IsNullOrWhiteSpace(productVersion))
        {
            var firstToken = productVersion.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            return string.IsNullOrWhiteSpace(firstToken) ? productVersion : firstToken.Trim();
        }

        return Normalize(identity.FileVersion);
    }

    private static OptiScalerVersionUpdateDecision Decision(
        OptiScalerVersionUpdateCode code,
        string installedDisplayVersion,
        string targetDisplayVersion)
    {
        return new OptiScalerVersionUpdateDecision
        {
            Code = code,
            InstalledDisplayVersion = installedDisplayVersion,
            TargetDisplayVersion = targetDisplayVersion
        };
    }

    private static bool HasTargetIdentity(OptiScalerVersionIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.FileVersion)
               || !string.IsNullOrWhiteSpace(identity.ProductVersion)
               || !string.IsNullOrWhiteSpace(identity.DisplayVersion);
    }

    private static string ResolveNumericVersionCandidate(OptiScalerVersionIdentity identity)
    {
        var fileVersion = Normalize(identity.FileVersion);
        if (!string.IsNullOrWhiteSpace(fileVersion))
        {
            return fileVersion;
        }

        return Normalize(identity.ProductVersion);
    }

    private static bool HasProductIdentity(OptiScalerVersionIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.ProductVersion);
    }

    private static bool HasSameProductIdentity(OptiScalerVersionIdentity left, OptiScalerVersionIdentity right)
    {
        return HasProductIdentity(left)
               && HasProductIdentity(right)
               && string.Equals(
                   Normalize(left.ProductVersion),
                   Normalize(right.ProductVersion),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static int? CompareVersionTuples(string left, string right)
    {
        var leftParts = ParseVersionTuple(left);
        var rightParts = ParseVersionTuple(right);
        if (leftParts.Length == 0 || rightParts.Length == 0)
        {
            return null;
        }

        var size = Math.Max(leftParts.Length, rightParts.Length);
        for (var index = 0; index < size; index++)
        {
            var leftValue = index < leftParts.Length ? leftParts[index] : 0;
            var rightValue = index < rightParts.Length ? rightParts[index] : 0;
            if (leftValue < rightValue) return -1;
            if (leftValue > rightValue) return 1;
        }

        return 0;
    }

    private static int[] ParseVersionTuple(string value)
    {
        var text = Normalize(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var match = VersionTokenRegex.Match(text);
        if (!match.Success)
        {
            return [];
        }

        var tokens = match.Value.Split(['.', ','], StringSplitOptions.RemoveEmptyEntries);
        var parsed = new List<int>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!int.TryParse(token, out var number))
            {
                return [];
            }

            parsed.Add(number);
        }

        return parsed.ToArray();
    }

    private static bool ContainsPreviewMarker(string value)
    {
        return value.Contains("-pre", StringComparison.OrdinalIgnoreCase)
               || value.Contains("preview", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsFinalMarker(string value)
    {
        return value.Contains("-final", StringComparison.OrdinalIgnoreCase)
               || value.Contains("stable", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim();
    }
}

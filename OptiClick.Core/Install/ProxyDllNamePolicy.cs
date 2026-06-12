namespace OptiClick.Core.Install;

public static class ProxyDllNamePolicy
{
    public const string InvalidPreferredProxyNameErrorCode = "invalid_preferred_proxy_name";
    public const string AsiProxyName = "OptiScaler.asi";

    private static readonly string[] NormalProxyChain =
    [
        "dxgi.dll",
        "winmm.dll",
        "version.dll",
        "d3d12.dll"
    ];

    public static bool TryResolvePreferredStart(string? preferredName, out string canonicalPreferred, out string errorCode)
    {
        var normalized = (preferredName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            canonicalPreferred = NormalProxyChain[0];
            errorCode = "";
            return true;
        }

        if (string.Equals(normalized, AsiProxyName, StringComparison.OrdinalIgnoreCase))
        {
            canonicalPreferred = AsiProxyName;
            errorCode = "";
            return true;
        }

        var match = NormalProxyChain.FirstOrDefault(candidate =>
            string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match))
        {
            canonicalPreferred = match;
            errorCode = "";
            return true;
        }

        canonicalPreferred = "";
        errorCode = InvalidPreferredProxyNameErrorCode;
        return false;
    }

    public static IReadOnlyList<string> BuildCandidateChainForPreferred(string? preferredName)
    {
        if (!TryResolvePreferredStart(preferredName, out var canonicalPreferred, out _))
        {
            return [];
        }

        return BuildCandidateChainFromCanonical(canonicalPreferred);
    }

    public static IReadOnlyList<string> BuildCandidateChainFromCanonical(string canonicalPreferred)
    {
        if (string.Equals(canonicalPreferred, AsiProxyName, StringComparison.Ordinal))
        {
            return [AsiProxyName];
        }

        var startIndex = Array.FindIndex(
            NormalProxyChain,
            candidate => string.Equals(candidate, canonicalPreferred, StringComparison.Ordinal));
        if (startIndex < 0)
        {
            return [];
        }

        return NormalProxyChain.Skip(startIndex).ToArray();
    }
}

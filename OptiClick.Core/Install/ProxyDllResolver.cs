using OptiClick.Core.Models;

namespace OptiClick.Core.Install;

public sealed class ProxyDllResolver
{
    private static readonly string[] NormalProxyChain =
    [
        "dxgi.dll",
        "winmm.dll",
        "version.dll",
        "d3d12.dll"
    ];

    public static readonly IReadOnlyList<string> ManagedCandidateNames = new[]
    {
        "OptiScaler.asi",
        "OptiScaler.dll",
        "dxgi.dll",
        "winmm.dll",
        "d3d12.dll",
        "dbghelp.dll",
        "version.dll",
        "wininet.dll",
        "winhttp.dll"
    };

    public ProxyDllResolutionResult Resolve(
        GameEntry game,
        ExistingFileSnapshot snapshot,
        UalMode ualMode,
        string precheckResolvedName = "",
        string plannedResolvedDllName = "")
    {
        if (!TryResolvePreferredStart(game.OptiScalerDllName, out var preferredStart))
        {
            return new ProxyDllResolutionResult
            {
                Success = false,
                UseUal = ualMode != UalMode.None,
                UalMode = ualMode,
                FailureReason = "invalid_preferred_proxy_name"
            };
        }

        var candidates = BuildCandidates(preferredStart);
        var backups = new List<string>();
        var skipped = new List<string>();

        foreach (var candidate in candidates)
        {
            var existing = snapshot.Find(candidate);
            if (existing is null)
            {
                return new ProxyDllResolutionResult
                {
                    FinalDllName = candidate,
                    UseUal = ualMode != UalMode.None,
                    UalMode = ualMode,
                    BackupCandidates = backups,
                    SkippedCandidates = skipped
                };
            }

            if (existing.OwnerKind == DllOwnerKind.OptiScaler)
            {
                backups.Add(candidate);
                return new ProxyDllResolutionResult
                {
                    FinalDllName = candidate,
                    UseUal = ualMode != UalMode.None,
                    UalMode = ualMode,
                    BackupCandidates = backups,
                    SkippedCandidates = skipped
                };
            }

            skipped.Add(candidate);
        }

        return new ProxyDllResolutionResult
        {
            Success = false,
            UseUal = ualMode != UalMode.None,
            UalMode = ualMode,
            BackupCandidates = backups,
            SkippedCandidates = skipped,
            FailureReason = "proxy_candidate_unavailable"
        };
    }

    private static bool TryResolvePreferredStart(string preferred, out string canonicalPreferred)
    {
        var normalized = (preferred ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            canonicalPreferred = NormalProxyChain[0];
            return true;
        }

        if (string.Equals(normalized, "OptiScaler.asi", StringComparison.OrdinalIgnoreCase))
        {
            canonicalPreferred = "OptiScaler.asi";
            return true;
        }

        var match = NormalProxyChain.FirstOrDefault(candidate =>
            string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match))
        {
            canonicalPreferred = match;
            return true;
        }

        canonicalPreferred = "";
        return false;
    }

    private static IReadOnlyList<string> BuildCandidates(string canonicalPreferred)
    {
        if (string.Equals(canonicalPreferred, "OptiScaler.asi", StringComparison.Ordinal))
        {
            return ["OptiScaler.asi"];
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

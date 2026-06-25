using OptiClick.Core.Models;

namespace OptiClick.Core.Install;

public sealed class ProxyDllResolver
{
    public static readonly IReadOnlyList<string> ManagedCandidateNames = new[]
    {
        ProxyDllNamePolicy.AsiProxyName,
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
        ExistingFileSnapshot snapshot)
    {
        if (!ProxyDllNamePolicy.TryResolvePreferredStart(game.OptiScalerDllName, out var preferredStart, out _))
        {
            return new ProxyDllResolutionResult
            {
                Success = false,
                FailureReason = ProxyDllNamePolicy.InvalidPreferredProxyNameErrorCode
            };
        }

        var candidates = ProxyDllNamePolicy.BuildCandidateChainFromCanonical(preferredStart);
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
                    BackupCandidates = backups,
                    SkippedCandidates = skipped
                };
            }

            skipped.Add(candidate);
        }

        return new ProxyDllResolutionResult
        {
            Success = false,
            BackupCandidates = backups,
            SkippedCandidates = skipped,
            FailureReason = "proxy_candidate_unavailable"
        };
    }

}

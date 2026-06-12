using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallTargetResolver
{
    public CoreInstallPlanTargets ResolveTargets(CoreInstallPlanBuildInput input)
    {
        if (input is null)
        {
            return CoreInstallPlanTargets.Empty;
        }

        return new CoreInstallPlanTargets
        {
            TargetFolder = ResolveTargetFolder(input),
            MatchedExe = ResolveMatchedExe(input),
            FinalProxyDllName = ResolveFinalProxyDllName(input),
            GameDisplayName = ResolveGameDisplayName(input),
            ExcludeListPatterns = ResolveExcludeListPatterns(input.GameDescriptor)
        };
    }

    private static string ResolveTargetFolder(CoreInstallPlanBuildInput input)
    {
        var match = input.MatchSnapshot;
        var targetCandidate = string.IsNullOrWhiteSpace(match?.FolderPath)
            ? input.TargetFolderHint
            : match?.FolderPath;
        return InstallTargetPathPolicy.NormalizeTargetDirectory(targetCandidate);
    }

    private static string ResolveMatchedExe(CoreInstallPlanBuildInput input)
    {
        var match = input.MatchSnapshot;
        if (!string.IsNullOrWhiteSpace(match?.MatchedExe))
        {
            return match.MatchedExe.Trim();
        }

        if (!string.IsNullOrWhiteSpace(input.MatchedExeHint))
        {
            return input.MatchedExeHint.Trim();
        }

        return (input.GameDescriptor?.MatchExe ?? "").Trim();
    }

    private static string ResolveFinalProxyDllName(CoreInstallPlanBuildInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            return input.Precheck.ResolvedDllName.Trim();
        }

        if (ProxyDllNamePolicy.TryResolvePreferredStart(
                input.GameDescriptor?.OptiScalerDllName,
                out var preferredStart,
                out _))
        {
            return preferredStart;
        }

        return "";
    }

    private static IReadOnlyList<string> ResolveExcludeListPatterns(InstallGameDescriptor? game)
    {
        if (game?.ExcludeListPatterns?.Count > 0)
        {
            return InstallExcludeListPatternParser.Normalize(game.ExcludeListPatterns);
        }

        var raw = (game?.ExcludeListRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return InstallExcludeListPatternParser.Parse(raw);
    }

    private static string ResolveGameDisplayName(CoreInstallPlanBuildInput input)
    {
        var game = input.GameDescriptor;
        if (game is null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(game.DisplayName))
        {
            return game.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(game.GameNameEn))
        {
            return game.GameNameEn.Trim();
        }

        if (!string.IsNullOrWhiteSpace(game.GameNameKr))
        {
            return game.GameNameKr.Trim();
        }

        return (game.GameId ?? "").Trim();
    }
}

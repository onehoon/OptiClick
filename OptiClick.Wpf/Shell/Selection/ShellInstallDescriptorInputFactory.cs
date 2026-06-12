using OptiClick.Core.Install;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Shell.Selection;

internal static class ShellInstallDescriptorInputFactory
{
    public static InstallDescriptorInput FromShellGame(ShellGameCardModel? game)
    {
        var metadata = ShellGameInstallMetadataResolver.ResolveEffective(game);
        return ShellInstallDescriptorInputMapper.FromShellGame(game, metadata);
    }

    public static ResolvedInstallGameInputs ResolveInputs(ShellGameCardModel? game)
    {
        return ResolvedInstallGameInputsMapper.FromInput(FromShellGame(game));
    }

    // Keep one shared path for descriptor resolution in selection and execution flows.
    // Previously prepared inputs are treated as a stable flow snapshot when available.
    // If no prepared inputs exist, resolve fresh values from the selected game.
    public static ResolvedInstallGameInputs ResolveInputs(ShellGameCardModel? game, ResolvedInstallGameInputs? fallbackInputs)
    {
        if (game is null)
        {
            return fallbackInputs ?? ResolvedInstallGameInputs.Empty;
        }

        if (!string.IsNullOrWhiteSpace(fallbackInputs?.GameId) || !string.IsNullOrWhiteSpace(fallbackInputs?.MatchExe))
        {
            return fallbackInputs!;
        }

        var resolvedFromGame = ResolveInputs(game);
        if (!string.IsNullOrWhiteSpace(resolvedFromGame.GameId) || !string.IsNullOrWhiteSpace(resolvedFromGame.MatchExe))
        {
            return resolvedFromGame;
        }

        return fallbackInputs ?? ResolvedInstallGameInputs.Empty;
    }
}

using OptiClick.Wpf.Shell.Scan;

namespace OptiClick.Wpf.Shell.Games.Actions;

public sealed class ShellGameActionAvailabilityResolver
{
    public ShellGameActionAvailability Resolve(
        ShellGameCardModel? selectedGame,
        ShellGameMatchResult? matchResult)
    {
        if (selectedGame is null)
        {
            return Disabled(ShellGameActionReasonCodes.NoSelectedGame);
        }

        if (!selectedGame.Enabled)
        {
            return Disabled(ShellGameActionReasonCodes.Disabled);
        }

        if (matchResult is null || matchResult.Status == ShellGameMatchStatus.None)
        {
            return Disabled(ShellGameActionReasonCodes.ScanRequired);
        }

        if (matchResult.Status == ShellGameMatchStatus.UnsupportedGpu)
        {
            return Disabled(ShellGameActionReasonCodes.UnsupportedGpu);
        }

        if (matchResult.Status == ShellGameMatchStatus.Matched)
        {
            return new ShellGameActionAvailability
            {
                InstallEnabled = true,
                ReasonCode = ShellGameActionReasonCodes.None
            };
        }

        return Disabled(ShellGameActionReasonCodes.ScanRequired);
    }

    private static ShellGameActionAvailability Disabled(string reasonCode)
    {
        return new ShellGameActionAvailability
        {
            InstallEnabled = false,
            ReasonCode = reasonCode
        };
    }
}

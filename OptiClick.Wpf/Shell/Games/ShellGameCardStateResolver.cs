using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCardStateResolver : IShellGameCardStateResolver
{
    private readonly IGameSupportPolicy _gameSupportPolicy;

    public ShellGameCardStateResolver()
        : this(new GameSupportPolicy())
    {
    }

    public ShellGameCardStateResolver(IGameSupportPolicy gameSupportPolicy)
    {
        _gameSupportPolicy = gameSupportPolicy;
    }

    public ShellGameCardStateDecision Resolve(ShellGameCardModel game, RuntimeContext? runtimeContext)
    {
        if (game is null)
        {
            return new ShellGameCardStateDecision
            {
                State = ShellGameCardState.Unknown
            };
        }

        if (!game.Enabled)
        {
            return new ShellGameCardStateDecision
            {
                State = ShellGameCardState.Disabled,
                ReasonCode = GameSupportReasonCodes.EnabledFalse
            };
        }

        var vendor = GpuVendorDetector.DetectFromRuntimeContext(runtimeContext);
        if (vendor == GpuVendor.Unknown)
        {
            var decision = _gameSupportPolicy.Evaluate(game, runtimeContext);
            if (decision.IsSupported)
            {
                return new ShellGameCardStateDecision
                {
                    State = ShellGameCardState.Ready,
                    ReasonCode = decision.ReasonCode
                };
            }

            return new ShellGameCardStateDecision
            {
                State = ShellGameCardState.PendingScan,
                ReasonCode = decision.ReasonCode is GameSupportReasonCodes.UnknownGpu ? GameSupportReasonCodes.UnknownGpu : decision.ReasonCode
            };
        }

        var supportDecision = _gameSupportPolicy.Evaluate(game, runtimeContext);
        if (!supportDecision.IsSupported)
        {
            return new ShellGameCardStateDecision
            {
                State = ShellGameCardState.UnsupportedGpu,
                ReasonCode = supportDecision.ReasonCode
            };
        }

        return new ShellGameCardStateDecision
        {
            State = ShellGameCardState.Ready,
            ReasonCode = supportDecision.ReasonCode
        };
    }

}

namespace OptiClick.Wpf.Shell.Games;

public sealed class ShellGameCardStateDecision
{
    public ShellGameCardState State { get; init; } = ShellGameCardState.Unknown;
    public string ReasonCode { get; init; } = "";
}

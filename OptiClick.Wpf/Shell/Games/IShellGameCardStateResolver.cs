using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Games;

public interface IShellGameCardStateResolver
{
    ShellGameCardStateDecision Resolve(ShellGameCardModel game, RuntimeContext? runtimeContext);
}

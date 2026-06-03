using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Shell.Games.Support;

public interface IGameSupportPolicy
{
    GameSupportDecision Evaluate(ShellGameCardModel game, RuntimeContext? runtimeContext);
}

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Games;

public interface IRuntimeDataShellGameMapper
{
    ShellGameCatalog Map(RemoteRuntimeData runtimeData, AppLanguage language);
}

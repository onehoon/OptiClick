using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;

internal interface IMainShellCommandInteractionAccess
{
    AppStrings Strings { get; }
    AppLanguage SelectedLanguage { get; }
    RuntimeContext LatestRuntimeContext { get; }
    void ApplyStateUpdate(MainViewModelStateUpdate update);
    void ApplyDeferredStateUpdate(MainViewModelStateUpdate update);
}

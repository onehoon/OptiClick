using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;

internal sealed record MainShellCommandInteractionContext
{
    public required string CurrentAppVersion { get; init; }
    public required RuntimeContext RuntimeContext { get; init; }
    public required AppLanguage SelectedLanguage { get; init; }
    public required AppStrings Strings { get; init; }
    public required string LogDirectory { get; init; }
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Action<AppDialogRequest> ShowDeferredDialog { get; init; }
    public required Action<bool, bool, string?, string?> ApplyAppLog { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
}

using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;

internal sealed record MainShellCommandInteractionContextInput
{
    public required MainViewModelResultApplier ResultApplier { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Func<AppLanguage> ReadSelectedLanguage { get; init; }
    public required Func<RuntimeContext> ReadLatestRuntimeContext { get; init; }
    public required Func<string> ReadCurrentAppVersion { get; init; }
    public required Func<string> ReadLogDirectory { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action<AppDialogRequest> ShowDeferredDialog { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyStateUpdate { get; init; }
    public required Action<MainViewModelStateUpdate> ApplyDeferredStateUpdate { get; init; }
    public required Action<bool, bool, string?, string?> ApplyAppLog { get; init; }
}

using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record ShellSupportModuleCompositionServices
{
    public required SupportActionController SupportActionController { get; init; }
    public required SupportIssueContextBuilder SupportIssueContextBuilder { get; init; }
    public required ShellCommandActionController ShellCommandActionController { get; init; }
}

internal static class ShellSupportModuleComposition
{
    public static ShellSupportModuleCompositionServices Compose(
        MainViewModelAppDependencies appDependencies,
        MainViewModelAppFallbackServices fallbackServices,
        StartupModuleCompositionServices startupComposition)
    {
        ArgumentNullException.ThrowIfNull(appDependencies);
        ArgumentNullException.ThrowIfNull(fallbackServices);
        ArgumentNullException.ThrowIfNull(startupComposition);

        var supportIssueContextBuilder = appDependencies.SupportIssueContextBuilder ?? new SupportIssueContextBuilder();
        var contactIssueLinkBuilder = appDependencies.ContactIssueLinkBuilder ?? new ContactIssueLinkBuilder();
        var supportActionController = appDependencies.SupportActionController
                                      ?? new SupportActionController(
                                          contactIssueLinkBuilder,
                                          fallbackServices.ExternalUrlLauncher);
        var shellCommandActionController = appDependencies.ShellCommandActionController
                                           ?? new ShellCommandActionController(
                                               startupComposition.StartupNoticePresenter,
                                               supportIssueContextBuilder,
                                               supportActionController);

        return new ShellSupportModuleCompositionServices
        {
            SupportActionController = supportActionController,
            SupportIssueContextBuilder = supportIssueContextBuilder,
            ShellCommandActionController = shellCommandActionController
        };
    }
}

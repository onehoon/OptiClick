using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;

namespace OptiClick.Wpf.ViewModels.Features.Shell.StartupAnnouncement;

internal sealed class MainStartupAnnouncementInteractionFeature
{
    private readonly StartupAnnouncementFlowController _controller;
    private readonly MainShellInteractionContextFactory _contextFactory;

    public MainStartupAnnouncementInteractionFeature(
        StartupAnnouncementFlowController controller,
        MainShellInteractionContextFactory contextFactory)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task ShowStartupAnnouncementIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var context = _contextFactory.CreateStartupAnnouncementContext();
        var result = _controller.Build(new StartupAnnouncementFlowRequest
        {
            RuntimeData = context.RuntimeData,
            Language = context.Language,
            SelectedGpuVendor = context.SelectedGpuVendor
        });
        context.DispatchFlowLogs(result.Logs, MainViewModelLogCategories.Startup);

        OpenStartupUrlIfNeeded(context, result.StartupUrl);

        if (!result.ShouldShowDialog || result.DialogRequest is null)
        {
            return;
        }

        await context.ShowDialogAsync(result.DialogRequest, cancellationToken);
    }

    private static void OpenStartupUrlIfNeeded(MainStartupAnnouncementInteractionContext context, string startupUrl)
    {
        var normalizedUrl = (startupUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            return;
        }

        try
        {
            if (!context.OpenExternalUrl(normalizedUrl))
            {
                context.LogWarning("startup_url open result=failed");
            }
        }
        catch (Exception ex)
        {
            context.LogWarning($"startup_url open result=failed type={ex.GetType().Name}");
        }
    }
}

using OptiClick.Wpf.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public sealed class OverlayInstallManagementDialogService : IInstallManagementDialogService
{
    private readonly InstallManagementDialogHostViewModel _dialogHost;

    public OverlayInstallManagementDialogService(InstallManagementDialogHostViewModel dialogHost)
    {
        _dialogHost = dialogHost ?? throw new ArgumentNullException(nameof(dialogHost));
    }

    public Task<InstallManagementDialogResult> ShowDialogAsync(
        InstallManagementDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        return _dialogHost.ShowAsync(request, cancellationToken);
    }
}

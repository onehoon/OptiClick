using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public interface IInstallManagementDialogService
{
    Task<InstallManagementDialogResult> ShowDialogAsync(
        InstallManagementDialogRequest request,
        CancellationToken cancellationToken = default);
}

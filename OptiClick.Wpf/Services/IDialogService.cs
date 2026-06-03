using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Services;

public interface IDialogService
{
    Task<AppDialogResult> ShowDialogAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken = default);
}

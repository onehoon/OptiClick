using OptiClick.Wpf.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public sealed class OverlayDialogService : IDialogService
{
    private readonly DialogHostViewModel _dialogHost;

    public OverlayDialogService(DialogHostViewModel dialogHost)
    {
        _dialogHost = dialogHost ?? throw new ArgumentNullException(nameof(dialogHost));
    }

    public Task<AppDialogResult> ShowDialogAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        return _dialogHost.ShowAsync(request, cancellationToken);
    }
}

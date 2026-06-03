using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.Dialogs;

public sealed class DialogPresenter
{
    private readonly IDialogService _dialogService;
    private readonly IAppLogger _appLogger;

    public DialogPresenter(IDialogService dialogService, IAppLogger? appLogger = null)
    {
        _dialogService = dialogService;
        _appLogger = appLogger ?? NullAppLogger.Instance;
    }

    public Task<AppDialogResult> ShowSafelyAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        return ShowSafelyCoreAsync(request, cancellationToken);
    }

    public void ShowDeferred(AppDialogRequest request)
    {
        _ = ShowSafelyAsync(request);
    }

    private async Task<AppDialogResult> ShowSafelyCoreAsync(
        AppDialogRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dialogService.ShowDialogAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _appLogger.Error("dialog", $"dialog show failed title={NormalizeStatusCode(request.Title, "untitled")}", ex);
            return AppDialogResult.None;
        }
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Navigation;

internal sealed class OptiScalerDirtyNavigationGuard
{
    public async Task<bool> ConfirmAsync(
        OptiScalerDirtyNavigationGuardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CurrentView != ShellViewKind.OptiScaler
            || request.TargetView == ShellViewKind.OptiScaler
            || !request.HasUnsavedChanges)
        {
            return true;
        }

        var result = await request.ShowDialogAsync(
            BuildDiscardChangesDialog(request.Text),
            cancellationToken);

        if (result == AppDialogResult.Ok)
        {
            request.SaveChanges();
            return true;
        }

        if (result == AppDialogResult.Continue)
        {
            request.DiscardChanges();
            return true;
        }

        return false;
    }

    private static AppDialogRequest BuildDiscardChangesDialog(OptiScalerDirtyNavigationGuardText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new AppDialogRequest
        {
            Kind = AppDialogKind.Warning,
            Severity = DialogSeverity.Warning,
            Title = text.Title,
            Summary = text.Summary,
            PrimaryButtonText = text.PrimaryButtonText,
            SecondaryButtonText = text.SecondaryButtonText,
            PrimaryResult = AppDialogResult.Ok,
            SecondaryResult = AppDialogResult.Continue,
            CanClose = true
        };
    }
}

internal sealed record OptiScalerDirtyNavigationGuardRequest
{
    public required ShellViewKind CurrentView { get; init; }
    public required ShellViewKind TargetView { get; init; }
    public required bool HasUnsavedChanges { get; init; }
    public required OptiScalerDirtyNavigationGuardText Text { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task<AppDialogResult>> ShowDialogAsync { get; init; }
    public required Action SaveChanges { get; init; }
    public required Action DiscardChanges { get; init; }
}

internal sealed record OptiScalerDirtyNavigationGuardText
{
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string PrimaryButtonText { get; init; }
    public required string SecondaryButtonText { get; init; }
}

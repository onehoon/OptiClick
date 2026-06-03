using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Support;

namespace OptiClick.Wpf.Composition;

public sealed record SupportCompositionServices
{
    public required IContactIssueLinkBuilder ContactIssueLinkBuilder { get; init; }
    public required SupportActionController SupportActionController { get; init; }
    public required SupportIssueContextBuilder SupportIssueContextBuilder { get; init; }
    public required GameDetailsDialogPresenter GameDetailsDialogPresenter { get; init; }
}

public sealed class SupportComposition
{
    private readonly AppCompositionRoot _root;

    public SupportComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SupportCompositionServices CreateSupportServices(AppSharedServices app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var contactIssueLinkBuilder = _root.CreateContactIssueLinkBuilder();
        return new SupportCompositionServices
        {
            ContactIssueLinkBuilder = contactIssueLinkBuilder,
            SupportActionController = new SupportActionController(
                contactIssueLinkBuilder,
                app.ExternalUrlLauncher),
            SupportIssueContextBuilder = new SupportIssueContextBuilder(),
            GameDetailsDialogPresenter = new GameDetailsDialogPresenter()
        };
    }
}

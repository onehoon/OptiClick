using OptiClick.Wpf.Models;

namespace OptiClick.Wpf.Shell.Dialogs.Markup;

public sealed record PopupMarkupDialogPresentation
{
    public PopupMarkupParseResult Summary { get; init; } = PopupMarkupParseResult.Empty;
    public IReadOnlyList<PopupMarkupBulletItem> BulletItems { get; init; } = [];
}

public static class PopupMarkupDialogPresentationBuilder
{
    public static PopupMarkupDialogPresentation Build(AppDialogRequest? request)
    {
        var summaryMarkup = PopupMarkupParser.Parse(request?.Summary);
        var mergedBullets = new List<PopupMarkupBulletItem>();

        mergedBullets.AddRange(summaryMarkup.BulletItems);

        var sourceBullets = request?.BulletItems ?? Array.Empty<string>();
        foreach (var bullet in sourceBullets)
        {
            var parsedBullet = PopupMarkupParser.Parse(bullet);
            if (parsedBullet.BulletItems.Count > 0)
            {
                mergedBullets.AddRange(parsedBullet.BulletItems);
            }

            if (!string.IsNullOrWhiteSpace(parsedBullet.PlainText))
            {
                mergedBullets.Add(new PopupMarkupBulletItem
                {
                    Inline = parsedBullet.Inline,
                    PlainText = parsedBullet.PlainText
                });
            }
        }

        return new PopupMarkupDialogPresentation
        {
            Summary = summaryMarkup,
            BulletItems = mergedBullets
        };
    }
}

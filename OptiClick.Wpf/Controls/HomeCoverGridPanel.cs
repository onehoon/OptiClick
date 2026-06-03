using System.Windows;
using System.Windows.Controls;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Controls;

public sealed class HomeCoverGridPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = ResolveAvailableWidth(availableSize.Width);
        var layout = HomeCoverGridLayoutCalculator.Calculate(availableWidth);
        var childSize = new Size(layout.CoverWidth, layout.CoverHeight);

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childSize);
        }

        return CalculatePanelSize(availableWidth, layout);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var availableWidth = ResolveAvailableWidth(finalSize.Width);
        var layout = HomeCoverGridLayoutCalculator.Calculate(availableWidth);
        var childWidth = layout.CoverWidth;
        var childHeight = layout.CoverHeight;

        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var column = index % layout.Columns;
            var row = index / layout.Columns;
            var x = column * (childWidth + layout.ColumnGap);
            var y = row * (childHeight + layout.RowGap);
            InternalChildren[index].Arrange(new Rect(x, y, childWidth, childHeight));
        }

        var panelSize = CalculatePanelSize(availableWidth, layout);
        return new Size(Math.Max(finalSize.Width, panelSize.Width), panelSize.Height);
    }

    private Size CalculatePanelSize(double availableWidth, HomeCoverGridLayout layout)
    {
        var rows = ResolveRows(layout.Columns);
        var height = rows <= 0
            ? 0
            : (layout.CoverHeight * rows) + (layout.RowGap * (rows - 1));
        var width = availableWidth > 0
            ? Math.Max(layout.TotalWidth, availableWidth)
            : layout.TotalWidth;

        return new Size(Math.Round(width, 2), Math.Round(height, 2));
    }

    private int ResolveRows(int columns)
    {
        if (columns <= 0 || InternalChildren.Count <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)InternalChildren.Count / columns);
    }

    private double ResolveAvailableWidth(double width)
    {
        var normalizedWidth = NormalizeWidth(width);
        return normalizedWidth > 0
            ? normalizedWidth
            : NormalizeWidth(ActualWidth);
    }

    private static double NormalizeWidth(double width)
    {
        return !double.IsNaN(width) && !double.IsInfinity(width) && width > 0
            ? width
            : 0;
    }
}

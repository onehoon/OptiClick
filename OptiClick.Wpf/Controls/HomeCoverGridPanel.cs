using System.Windows;
using System.Windows.Controls;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Controls;

public sealed class HomeCoverGridPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = ResolveAvailableWidth(availableSize.Width);
        var layoutWidth = HomeCoverGridLayoutCalculator.AdjustAvailableWidthForReservedCoverBorder(availableWidth);
        var layout = HomeCoverGridLayoutCalculator.Calculate(layoutWidth);
        var childSize = new Size(
            HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverWidth),
            HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverHeight));

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childSize);
        }

        return CalculatePanelSize(availableWidth, layout);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var availableWidth = ResolveAvailableWidth(finalSize.Width);
        var layoutWidth = HomeCoverGridLayoutCalculator.AdjustAvailableWidthForReservedCoverBorder(availableWidth);
        var layout = HomeCoverGridLayoutCalculator.Calculate(layoutWidth);
        var childWidth = HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverWidth);
        var childHeight = HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverHeight);
        var columnGap = HomeCoverGridLayoutCalculator.AdjustGapForReservedCoverBorder(layout.ColumnGap);
        var rowGap = HomeCoverGridLayoutCalculator.AdjustGapForReservedCoverBorder(layout.RowGap);

        for (var index = 0; index < InternalChildren.Count; index++)
        {
            var column = index % layout.Columns;
            var row = index / layout.Columns;
            var x = column * (childWidth + columnGap);
            var y = row * (childHeight + rowGap);
            InternalChildren[index].Arrange(new Rect(x, y, childWidth, childHeight));
        }

        var panelSize = CalculatePanelSize(availableWidth, layout);
        return new Size(Math.Max(finalSize.Width, panelSize.Width), panelSize.Height);
    }

    private Size CalculatePanelSize(double availableWidth, HomeCoverGridLayout layout)
    {
        var rows = ResolveRows(layout.Columns);
        var columnGap = HomeCoverGridLayoutCalculator.AdjustGapForReservedCoverBorder(layout.ColumnGap);
        var rowGap = HomeCoverGridLayoutCalculator.AdjustGapForReservedCoverBorder(layout.RowGap);
        var coverWidth = HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverWidth);
        var coverHeight = HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverHeight);
        var totalWidth = Math.Round((coverWidth * layout.Columns) + (columnGap * Math.Max(0, layout.Columns - 1)), 2);
        var height = rows <= 0
            ? 0
            : (coverHeight * rows) + (rowGap * (rows - 1));
        var width = availableWidth > 0
            ? Math.Max(totalWidth, availableWidth)
            : totalWidth;

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

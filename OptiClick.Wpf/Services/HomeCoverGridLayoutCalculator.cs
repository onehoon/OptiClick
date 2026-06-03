namespace OptiClick.Wpf.Services;

internal readonly record struct HomeCoverGridLayout(
    int Columns,
    double CoverWidth,
    double ColumnGap,
    double RowGap)
{
    public double CoverHeight => Math.Round(CoverWidth * HomeCoverGridLayoutCalculator.CoverAspectHeightPerWidth, 2);
    public double TotalWidth => Math.Round((CoverWidth * Columns) + (ColumnGap * Math.Max(0, Columns - 1)), 2);
}

internal static class HomeCoverGridLayoutCalculator
{
    public const double MaxCoverWidthDip = 180;
    public const double MinCoverWidthDip = 120;
    public const double CoverGapDip = 10;
    public const double MinCoverGapDip = CoverGapDip;
    public const double MaxCoverGapDip = 24;
    public const double CoverAspectHeightPerWidth = 1.5;

    public static HomeCoverGridLayout Calculate(
        double availableWidth,
        double maxCoverWidth = MaxCoverWidthDip,
        double minCoverWidth = MinCoverWidthDip,
        double minGap = MinCoverGapDip,
        double maxGap = MaxCoverGapDip)
    {
        var safeMax = Math.Max(1, maxCoverWidth);
        var safeMin = Math.Clamp(minCoverWidth, 1, safeMax);
        var safeMinGap = Math.Max(0, minGap);
        var safeMaxGap = Math.Max(safeMinGap, maxGap);

        if (availableWidth <= 0)
        {
            return new HomeCoverGridLayout(1, 0, safeMinGap, safeMinGap);
        }

        if (availableWidth <= safeMax)
        {
            return new HomeCoverGridLayout(
                1,
                Math.Round(Math.Max(1, availableWidth), 2),
                safeMinGap,
                safeMinGap);
        }

        var columnsAtMaxWidth = Math.Max(
            1,
            (int)Math.Floor((availableWidth + safeMinGap) / (safeMax + safeMinGap)));
        var maxColumnsByMinWidth = Math.Max(
            1,
            (int)Math.Floor((availableWidth + safeMinGap) / (safeMin + safeMinGap)));
        var columns = Math.Min(columnsAtMaxWidth, maxColumnsByMinWidth);

        if (columns == 1)
        {
            var leftoverAtMaxWidth = availableWidth - safeMax;
            if (leftoverAtMaxWidth > safeMaxGap && columns < maxColumnsByMinWidth)
            {
                columns++;
            }
        }

        if (columns > 1)
        {
            var gapAtMaxWidth = (availableWidth - (safeMax * columns)) / (columns - 1);
            if (gapAtMaxWidth >= safeMinGap && gapAtMaxWidth <= safeMaxGap)
            {
                return new HomeCoverGridLayout(
                    columns,
                    safeMax,
                    Math.Round(gapAtMaxWidth, 2),
                    Math.Round(gapAtMaxWidth, 2));
            }

            if (gapAtMaxWidth > safeMaxGap && columns < maxColumnsByMinWidth)
            {
                columns++;
            }
        }

        var coverWidth = (availableWidth - (safeMinGap * (columns - 1))) / columns;
        coverWidth = Math.Min(coverWidth, safeMax);
        coverWidth = columns > 1 ? Math.Max(coverWidth, safeMin) : Math.Max(1, coverWidth);

        return new HomeCoverGridLayout(
            columns,
            Math.Round(coverWidth, 2),
            safeMinGap,
            safeMinGap);
    }
}

namespace OptiClick.Wpf.Services;

internal readonly record struct HomeVisibleCoverLoadRange(
    int VisibleFirstIndex,
    int VisibleCount,
    int BufferFirstIndex,
    int BufferCount)
{
    public static HomeVisibleCoverLoadRange Empty { get; } = new(0, 0, 0, 0);
}

internal static class HomeVisibleCoverLoadRangeCalculator
{
    public static HomeVisibleCoverLoadRange Calculate(
        int itemCount,
        int columns,
        double verticalOffset,
        double viewportHeight,
        double rowStride,
        int bufferViewportCount = 1)
    {
        if (itemCount <= 0 || columns <= 0 || viewportHeight <= 0 || rowStride <= 0)
        {
            return HomeVisibleCoverLoadRange.Empty;
        }

        var safeOffset = Math.Max(0, verticalOffset);
        var firstVisibleRow = Math.Max(0, (int)Math.Floor(safeOffset / rowStride));
        var firstIndex = firstVisibleRow * columns;
        if (firstIndex >= itemCount)
        {
            return HomeVisibleCoverLoadRange.Empty;
        }

        var offsetWithinRow = safeOffset - (firstVisibleRow * rowStride);
        var visibleRows = Math.Max(1, (int)Math.Ceiling((offsetWithinRow + viewportHeight) / rowStride));
        var visibleCount = Math.Min(visibleRows * columns, itemCount - firstIndex);

        var bufferRows = visibleRows * Math.Max(0, bufferViewportCount);
        var bufferFirstIndex = firstIndex + visibleCount;
        var bufferCount = bufferRows <= 0 || bufferFirstIndex >= itemCount
            ? 0
            : Math.Min(bufferRows * columns, itemCount - bufferFirstIndex);

        return new HomeVisibleCoverLoadRange(
            firstIndex,
            visibleCount,
            bufferFirstIndex,
            bufferCount);
    }
}

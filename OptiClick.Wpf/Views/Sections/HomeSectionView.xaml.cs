using System.Diagnostics;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.ViewModels.Sections.Home;

namespace OptiClick.Wpf.Views.Sections;

public partial class HomeSectionView : UserControl
{
    private const double LayoutChangeEpsilon = 0.5;
    private readonly DispatcherTimer _resizeDebounceTimer;
    private readonly DispatcherTimer _visibleCoverLoadTimer;
    private bool _coverLayoutUpdatePending;
    private HomeSectionViewModel? _boundViewModel;
    private HomeCoverGridLayout? _lastAppliedLayout;
    private int _resizeEventCount;
    private int _layoutApplyCount;
    private int _coverLoadRequestVersion;

    public HomeSectionView()
    {
        InitializeComponent();

        _resizeDebounceTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _resizeDebounceTimer.Tick += ResizeDebounceTimer_OnTick;

        _visibleCoverLoadTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _visibleCoverLoadTimer.Tick += VisibleCoverLoadTimer_OnTick;

        Loaded += HomeSectionView_OnLoaded;
        Unloaded += HomeSectionView_OnUnloaded;
        DataContextChanged += HomeSectionView_OnDataContextChanged;
        IsVisibleChanged += HomeSectionView_OnIsVisibleChanged;
    }

    private void HomeSectionView_OnLoaded(object sender, RoutedEventArgs e)
    {
        BindViewModel(DataContext);
        ScheduleCoverGridLayoutUpdate();
        ScheduleVisibleCoverLoad();
    }

    private void HomeSectionView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _resizeDebounceTimer.Stop();
        _visibleCoverLoadTimer.Stop();
        _coverLayoutUpdatePending = false;
        _lastAppliedLayout = null;
        _coverLoadRequestVersion++;
        BindViewModel(null);
    }

    private void HomeSectionView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        BindViewModel(e.NewValue);
        ScheduleCoverGridLayoutUpdate();
        ScheduleVisibleCoverLoad();
    }

    private void HomeSectionView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        ScheduleCoverGridLayoutUpdate();
        ScheduleVisibleCoverLoad();
    }

    private void HomeGamesScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _resizeEventCount++;
        Debug.WriteLine($"[Resize] event={_resizeEventCount} width={e.NewSize.Width:0} height={e.NewSize.Height:0}");
        ScheduleDebouncedCoverGridLayoutUpdate();
    }

    private void HomeGamesScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 && e.ViewportHeightChange == 0 && e.ExtentHeightChange == 0)
        {
            return;
        }

        ScheduleVisibleCoverLoad();
    }

    private void Games_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleCoverGridLayoutUpdate();
        ScheduleVisibleCoverLoad();
    }

    private void BoundViewModel_OnVisibleCoverLoadRequested(object? sender, EventArgs e)
    {
        ScheduleVisibleCoverLoad();
    }

    private void ResizeDebounceTimer_OnTick(object? sender, EventArgs e)
    {
        _resizeDebounceTimer.Stop();
        if (TryApplyCoverGridLayout(force: false))
        {
            ScheduleVisibleCoverLoad();
        }
    }

    private void VisibleCoverLoadTimer_OnTick(object? sender, EventArgs e)
    {
        _visibleCoverLoadTimer.Stop();
        RequestVisibleCoverLoad(_coverLoadRequestVersion);
    }

    private void BindViewModel(object? dataContext)
    {
        if (ReferenceEquals(_boundViewModel, dataContext))
        {
            return;
        }

        if (_boundViewModel is not null)
        {
            _boundViewModel.Games.CollectionChanged -= Games_OnCollectionChanged;
            _boundViewModel.VisibleCoverLoadRequested -= BoundViewModel_OnVisibleCoverLoadRequested;
        }

        _boundViewModel = dataContext as HomeSectionViewModel;
        if (_boundViewModel is not null)
        {
            _boundViewModel.Games.CollectionChanged += Games_OnCollectionChanged;
            _boundViewModel.VisibleCoverLoadRequested += BoundViewModel_OnVisibleCoverLoadRequested;
        }
    }

    private void ScheduleCoverGridLayoutUpdate()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (TryApplyCoverGridLayout(force: true))
        {
            return;
        }

        if (_coverLayoutUpdatePending)
        {
            return;
        }

        _coverLayoutUpdatePending = true;
        _ = Dispatcher.InvokeAsync(() =>
        {
            _coverLayoutUpdatePending = false;
            TryApplyCoverGridLayout(force: true);
        }, DispatcherPriority.Render);
    }

    private void ScheduleDebouncedCoverGridLayoutUpdate()
    {
        if (!IsLoaded)
        {
            return;
        }

        _resizeDebounceTimer.Stop();
        _resizeDebounceTimer.Start();
    }

    private void ScheduleVisibleCoverLoad()
    {
        if (!IsLoaded || !IsVisible)
        {
            return;
        }

        _coverLoadRequestVersion++;
        _visibleCoverLoadTimer.Stop();
        _visibleCoverLoadTimer.Start();
    }

    private bool TryApplyCoverGridLayout(bool force)
    {
        if (_boundViewModel is null)
        {
            return false;
        }

        var availableWidth = ResolveCoverGridAvailableWidth();
        if (availableWidth <= 0)
        {
            return false;
        }

        var layoutWidth = HomeCoverGridLayoutCalculator.AdjustAvailableWidthForReservedCoverBorder(availableWidth);
        var layout = HomeCoverGridLayoutCalculator.Calculate(layoutWidth);
        var layoutChanged = HasLayoutChanged(layout);
        if (!force && !layoutChanged)
        {
            Debug.WriteLine($"[Layout] skipped reason=same_layout width={availableWidth:0}");
            return true;
        }

        var changedCards = 0;
        foreach (var game in _boundViewModel.Games)
        {
            if (game.ApplyCardSize(
                HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverWidth),
                HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverHeight)))
            {
                changedCards++;
            }
        }

        if (!layoutChanged && changedCards == 0 && _lastAppliedLayout is not null)
        {
            Debug.WriteLine($"[Layout] skipped reason=no_effective_change width={availableWidth:0}");
            return true;
        }

        _lastAppliedLayout = layout;
        if (force || layoutChanged || changedCards > 0)
        {
            Debug.WriteLine($"[Layout] apply={++_layoutApplyCount} columns={layout.Columns} cover_width={layout.CoverWidth:0.##} cover_height={layout.CoverHeight:0.##} changed_cards={changedCards}");
            HomeGamesItemsControl.InvalidateMeasure();
            HomeGamesScrollViewer.InvalidateScrollInfo();
        }

        return true;
    }

    private void RequestVisibleCoverLoad(int requestVersion)
    {
        if (!IsLoaded || !IsVisible || _boundViewModel is null || _boundViewModel.Games.Count == 0)
        {
            return;
        }

        var availableWidth = ResolveCoverGridAvailableWidth();
        if (availableWidth <= 0)
        {
            Debug.WriteLine("home_cover_reload skipped reason=layout_not_ready");
            return;
        }

        var layoutWidth = HomeCoverGridLayoutCalculator.AdjustAvailableWidthForReservedCoverBorder(availableWidth);
        var layout = HomeCoverGridLayoutCalculator.Calculate(layoutWidth);
        if (layout.Columns <= 0 || layout.CoverWidth <= 0 || layout.CoverHeight <= 0)
        {
            Debug.WriteLine("home_cover_reload skipped reason=layout_not_ready");
            return;
        }

        var viewportHeight = ResolveViewportHeight();
        if (viewportHeight <= 0)
        {
            Debug.WriteLine("home_cover_reload skipped reason=layout_not_ready");
            return;
        }

        var rowStride = HomeCoverGridLayoutCalculator.ExpandSizeForReservedCoverBorder(layout.CoverHeight)
                        + HomeCoverGridLayoutCalculator.AdjustGapForReservedCoverBorder(layout.RowGap);
        if (rowStride <= 0)
        {
            Debug.WriteLine("home_cover_reload skipped reason=layout_not_ready");
            return;
        }

        var range = HomeVisibleCoverLoadRangeCalculator.Calculate(
            _boundViewModel.Games.Count,
            layout.Columns,
            HomeGamesScrollViewer.VerticalOffset,
            viewportHeight,
            rowStride);
        if (range.VisibleCount <= 0)
        {
            return;
        }

        Debug.WriteLine($"home_visible_cover_load queued first={range.VisibleFirstIndex} count={range.VisibleCount} buffer_first={range.BufferFirstIndex} buffer_count={range.BufferCount}");

        var visibleResult = RefreshCoverRange(range.VisibleFirstIndex, range.VisibleCount);
        Debug.WriteLine($"home_visible_cover_load completed phase=visible updated={visibleResult.updated} skipped={visibleResult.skipped}");

        if (range.BufferCount > 0)
        {
            ScheduleBufferedCoverLoad(
                range.BufferFirstIndex,
                range.BufferCount,
                layout.Columns,
                requestVersion);
        }
    }

    private void ScheduleBufferedCoverLoad(int firstIndex, int count, int batchSize, int requestVersion)
    {
        var safeBatchSize = Math.Max(1, batchSize);
        _ = Dispatcher.InvokeAsync(() =>
        {
            if (requestVersion != _coverLoadRequestVersion || !IsLoaded || !IsVisible)
            {
                Debug.WriteLine("home_visible_cover_load skipped phase=buffer reason=stale_request");
                return;
            }

            var currentCount = Math.Min(safeBatchSize, count);
            var bufferResult = RefreshCoverRange(firstIndex, currentCount);
            Debug.WriteLine($"home_visible_cover_load completed phase=buffer first={firstIndex} count={currentCount} updated={bufferResult.updated} skipped={bufferResult.skipped}");

            var remainingCount = count - currentCount;
            if (remainingCount > 0)
            {
                ScheduleBufferedCoverLoad(
                    firstIndex + currentCount,
                    remainingCount,
                    safeBatchSize,
                    requestVersion);
            }
        }, DispatcherPriority.ContextIdle);
    }

    private (int updated, int skipped) RefreshCoverRange(int firstIndex, int count)
    {
        if (_boundViewModel is null || firstIndex < 0 || count <= 0 || firstIndex >= _boundViewModel.Games.Count)
        {
            return (0, 0);
        }

        var updated = 0;
        var skipped = 0;
        var lastIndexExclusive = Math.Min(firstIndex + count, _boundViewModel.Games.Count);
        for (var index = firstIndex; index < lastIndexExclusive; index++)
        {
            if (_boundViewModel.Games[index].RefreshCoverFromLocalCache())
            {
                updated++;
            }
            else
            {
                skipped++;
            }
        }

        return (updated, skipped);
    }

    private bool HasLayoutChanged(HomeCoverGridLayout layout)
    {
        if (_lastAppliedLayout is not { } previous)
        {
            return true;
        }

        return layout.Columns != previous.Columns
               || Math.Abs(layout.CoverWidth - previous.CoverWidth) >= LayoutChangeEpsilon
               || Math.Abs(layout.CoverHeight - previous.CoverHeight) >= LayoutChangeEpsilon
               || Math.Abs(layout.ColumnGap - previous.ColumnGap) >= LayoutChangeEpsilon
               || Math.Abs(layout.RowGap - previous.RowGap) >= LayoutChangeEpsilon;
    }

    private double ResolveCoverGridAvailableWidth()
    {
        var itemsWidth = NormalizeLayoutWidth(HomeGamesItemsControl.ActualWidth);
        if (itemsWidth > 0)
        {
            return itemsWidth;
        }

        var viewportWidth = NormalizeLayoutWidth(HomeGamesScrollViewer.ViewportWidth);
        if (viewportWidth > 0)
        {
            return viewportWidth;
        }

        var scrollViewerWidth = NormalizeLayoutWidth(HomeGamesScrollViewer.ActualWidth);
        return scrollViewerWidth > 0
            ? scrollViewerWidth
            : NormalizeLayoutWidth(HomeSectionRoot.ActualWidth);
    }

    private double ResolveViewportHeight()
    {
        var viewportHeight = NormalizeLayoutWidth(HomeGamesScrollViewer.ViewportHeight);
        if (viewportHeight > 0)
        {
            return viewportHeight;
        }

        var scrollViewerHeight = NormalizeLayoutWidth(HomeGamesScrollViewer.ActualHeight);
        return scrollViewerHeight > 0
            ? scrollViewerHeight
            : NormalizeLayoutWidth(HomeSectionRoot.ActualHeight);
    }

    private static double NormalizeLayoutWidth(double width)
    {
        return !double.IsNaN(width) && !double.IsInfinity(width) && width > 0
            ? width
            : 0;
    }
}

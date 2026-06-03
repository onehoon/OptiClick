using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OptiClick.Wpf.ViewModels.Sections.SupportedGames;

namespace OptiClick.Wpf.Views.Sections;

public partial class SupportedGamesWikiSectionView : UserControl
{
    private readonly DispatcherTimer _visibleCoverLoadTimer;

    public SupportedGamesWikiSectionView()
    {
        InitializeComponent();

        _visibleCoverLoadTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _visibleCoverLoadTimer.Tick += VisibleCoverLoadTimer_OnTick;

        Loaded += SupportedGamesWikiSectionView_OnLoaded;
        Unloaded += SupportedGamesWikiSectionView_OnUnloaded;
        DataContextChanged += SupportedGamesWikiSectionView_OnDataContextChanged;
        IsVisibleChanged += SupportedGamesWikiSectionView_OnIsVisibleChanged;
    }

    private void SupportedGamesWikiSectionView_OnLoaded(object sender, RoutedEventArgs e)
    {
        ScheduleVisibleCoverLoad();
    }

    private void SupportedGamesWikiSectionView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _visibleCoverLoadTimer.Stop();
    }

    private void SupportedGamesWikiSectionView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ScheduleVisibleCoverLoad();
    }

    private void SupportedGamesWikiSectionView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ScheduleVisibleCoverLoad();
    }

    private void SupportedGamesWikiScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 && e.ViewportHeightChange == 0 && e.ExtentHeightChange == 0)
        {
            return;
        }

        ScheduleVisibleCoverLoad();
    }

    private void SupportedGamesWikiScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScheduleVisibleCoverLoad();
    }

    private void VisibleCoverLoadTimer_OnTick(object? sender, EventArgs e)
    {
        _visibleCoverLoadTimer.Stop();
        RequestVisibleCoverLoad();
    }

    private void ScheduleVisibleCoverLoad()
    {
        if (!IsLoaded || !IsVisible)
        {
            return;
        }

        _visibleCoverLoadTimer.Stop();
        _visibleCoverLoadTimer.Start();
    }

    private void RequestVisibleCoverLoad()
    {
        if (DataContext is not SupportedGamesSectionViewModel viewModel)
        {
            return;
        }

        viewModel.QueueVisibleCoverLoad(
            SupportedGamesWikiScrollViewer.VerticalOffset,
            SupportedGamesWikiScrollViewer.ViewportHeight);
    }
}

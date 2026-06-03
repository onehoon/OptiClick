using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Services;
using OptiClick.Core.Models;
using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.ViewModels;

public sealed class GameCardViewModel : ViewModelBase
{
    private static readonly Brush SelectedBorder = new SolidColorBrush(Color.FromRgb(122, 162, 255));
    private static readonly Brush NormalBorder = new SolidColorBrush(Color.FromRgb(47, 57, 70));
    private static readonly Thickness SelectedThickness = new(3);
    private static readonly Thickness NormalThickness = new(0);
    private const string DefaultCoverImageSource = CoverImageCacheService.DefaultCoverImageSource;
    private const double CoverImageWidthRefreshThresholdDip = 12.0;
    private const double CoverImageHeightRefreshThresholdDip = 18.0;
    private static readonly Lazy<ImageSource> SharedDefaultCoverImage = new(CreateDefaultCoverImage);
    private static int CoverReloadCount;

    private readonly object _coverLoadGate = new();
    private bool _isSelected;
    private bool _isDimmed;
    private string _statusBadge;
    private string _coverImageSource = DefaultCoverImageSource;
    private ImageSource _coverImage = SharedDefaultCoverImage.Value;
    private double _cardWidth;
    private double _cardHeight;
    private bool _isCardLayoutReady;
    private int _coverLoadOperationId;
    private string _activeCoverLoadKey = "";
    private string _lastAppliedCoverSource = DefaultCoverImageSource;
    private double _lastAppliedCoverWidth;
    private double _lastAppliedCoverHeight;
    private Brush _badgeBrush;
    private Brush _badgeBorderBrush;
    private Brush _badgeForegroundBrush;

    public GameCardViewModel(
        string title,
        string subtitle,
        string statusBadge,
        string supportReasonCode,
        string optiScalerSummary,
        string componentSummary,
        string notePreview,
        Brush coverBrush,
        Brush badgeBrush,
        GameEntry gameEntry,
        ShellGameCardModel? sourceModel = null,
        Brush? badgeBorderBrush = null,
        Brush? badgeForegroundBrush = null)
    {
        Title = title;
        Subtitle = subtitle;
        _statusBadge = statusBadge ?? "";
        SupportReasonCode = supportReasonCode;
        OptiScalerSummary = optiScalerSummary;
        ComponentSummary = componentSummary;
        NotePreview = notePreview;
        CoverBrush = coverBrush;
        _badgeBrush = badgeBrush;
        _badgeBorderBrush = badgeBorderBrush ?? badgeBrush;
        _badgeForegroundBrush = badgeForegroundBrush ?? Brushes.White;
        GameEntry = gameEntry;
        SourceModel = sourceModel;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string StatusBadge
    {
        get => _statusBadge;
        private set => SetProperty(ref _statusBadge, value);
    }
    public string SupportReasonCode { get; }
    public string OptiScalerSummary { get; }
    public string ComponentSummary { get; }
    public string NotePreview { get; }
    public Brush CoverBrush { get; }
    public string CoverImageSource
    {
        get => _coverImageSource;
        private set => SetProperty(ref _coverImageSource, value);
    }
    public ImageSource CoverImage
    {
        get => _coverImage;
        private set => SetProperty(ref _coverImage, value);
    }
    public double CardWidth
    {
        get => _cardWidth;
        private set => SetProperty(ref _cardWidth, value);
    }
    public double CardHeight
    {
        get => _cardHeight;
        private set => SetProperty(ref _cardHeight, value);
    }
    public Brush BadgeBrush
    {
        get => _badgeBrush;
        private set => SetProperty(ref _badgeBrush, value);
    }
    public Brush BadgeBorderBrush
    {
        get => _badgeBorderBrush;
        private set => SetProperty(ref _badgeBorderBrush, value);
    }
    public Brush BadgeForegroundBrush
    {
        get => _badgeForegroundBrush;
        private set => SetProperty(ref _badgeForegroundBrush, value);
    }
    public GameEntry GameEntry { get; }
    public ShellGameCardModel? SourceModel { get; }
    public Brush CardBorderBrush => IsSelected ? SelectedBorder : NormalBorder;
    public Thickness CardBorderThickness => IsSelected ? SelectedThickness : NormalThickness;
    public double CardOpacity => IsDimmed ? 0.78 : 1.0;
    public Visibility CardLayoutVisibility => _isCardLayoutReady ? Visibility.Visible : Visibility.Collapsed;

    public bool RefreshCoverFromLocalCache()
    {
        if (!_isCardLayoutReady || CardWidth <= 0 || CardHeight <= 0)
        {
            Debug.WriteLine($"home_cover_reload skipped reason=layout_not_ready game={ResolveLogGameId()}");
            return false;
        }

        var coverLookup = ResolveCoverImageLookup(SourceModel);
        var source = ResolveCoverImageSource(SourceModel);
        var didApply = false;
        if (IsCurrentCoverImageRequest(source, CardWidth, CardHeight))
        {
            Debug.WriteLine($"home_cover_reload skipped reason=same_source game={ResolveLogGameId()}");
        }
        else
        {
            didApply = ApplyCoverImage(
                source,
                CreateCoverImage(source, coverLookup.sourceUrl, coverLookup.steamAppId),
                CardWidth,
                CardHeight);
        }

        if (IsHttpUrl(coverLookup.sourceUrl) && IsDefaultCoverSource(source))
        {
            BeginCoverLoadIfNeeded(coverLookup.sourceUrl, coverLookup.steamAppId);
        }

        return didApply;
    }

    public bool ApplyInstallStatusPresentationFrom(GameCardViewModel refreshedCard)
    {
        ArgumentNullException.ThrowIfNull(refreshedCard);

        var didChange = false;
        didChange |= SetProperty(ref _statusBadge, refreshedCard.StatusBadge ?? "", nameof(StatusBadge));
        didChange |= SetProperty(ref _badgeBrush, refreshedCard.BadgeBrush, nameof(BadgeBrush));
        didChange |= SetProperty(ref _badgeBorderBrush, refreshedCard.BadgeBorderBrush, nameof(BadgeBorderBrush));
        didChange |= SetProperty(ref _badgeForegroundBrush, refreshedCard.BadgeForegroundBrush, nameof(BadgeForegroundBrush));
        return didChange;
    }

    public bool ApplyCardSize(double width, double height)
    {
        var safeWidth = Math.Clamp(width, 1, SupportedGamesWikiLayoutProfile.MainCardWidthDip);
        var safeHeight = Math.Clamp(height, 1, SupportedGamesWikiLayoutProfile.MainCardHeightDip);
        var nextWidth = Math.Round(safeWidth, 2);
        var nextHeight = Math.Round(safeHeight, 2);

        if (Math.Abs(CardWidth - nextWidth) < 0.01
            && Math.Abs(CardHeight - nextHeight) < 0.01)
        {
            return false;
        }

        CardWidth = nextWidth;
        CardHeight = nextHeight;
        if (!_isCardLayoutReady)
        {
            _isCardLayoutReady = true;
            OnPropertyChanged(nameof(CardLayoutVisibility));
        }

        return true;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(CardBorderBrush));
                OnPropertyChanged(nameof(CardBorderThickness));
            }
        }
    }

    public bool IsDimmed
    {
        get => _isDimmed;
        set
        {
            if (SetProperty(ref _isDimmed, value))
            {
                OnPropertyChanged(nameof(CardOpacity));
            }
        }
    }

    private static string ResolveCoverImageSource(ShellGameCardModel? sourceModel)
    {
        if (sourceModel is null)
        {
            return DefaultCoverImageSource;
        }

        var coverUrl = (sourceModel.CoverUrl ?? "").Trim();
        var steamAppId = (sourceModel.CoverSteamAppId ?? "").Trim();
        return CoverImageCacheService.ResolveLocalCoverSourceOrDefault(coverUrl, steamAppId, DefaultCoverImageSource);
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    private static (string sourceUrl, string steamAppId) ResolveCoverImageLookup(ShellGameCardModel? sourceModel)
    {
        if (sourceModel is null)
        {
            return ("", "");
        }

        var coverUrl = (sourceModel.CoverUrl ?? "").Trim();
        var steamAppId = (sourceModel.CoverSteamAppId ?? "").Trim();

        if (IsHttpUrl(coverUrl))
        {
            return (coverUrl, steamAppId);
        }

        if (string.IsNullOrWhiteSpace(steamAppId))
        {
            return ("", "");
        }

        return (CoverImageCacheService.BuildSteamCoverUrl(steamAppId), steamAppId);
    }

    private void BeginCoverLoadIfNeeded(string sourceUrl, string steamAppId)
    {
        if (!IsHttpUrl(sourceUrl))
        {
            return;
        }

        var loadKey = BuildCoverLoadKey(sourceUrl, steamAppId);
        lock (_coverLoadGate)
        {
            if (string.Equals(_activeCoverLoadKey, loadKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeCoverLoadKey = loadKey;
        }

        var operationId = Interlocked.Increment(ref _coverLoadOperationId);
        _ = BeginCoverLoadAsync(sourceUrl, steamAppId, operationId, loadKey, CardWidth, CardHeight);
    }

    private async Task BeginCoverLoadAsync(
        string sourceUrl,
        string steamAppId,
        int operationId,
        string loadKey,
        double targetWidth,
        double targetHeight)
    {
        try
        {
            var cachedPath = await CoverImageCacheService.EnsureCachedAsync(sourceUrl, steamAppId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                return;
            }

            if (operationId != Volatile.Read(ref _coverLoadOperationId))
            {
                return;
            }

            var cachedUri = SafeAbsoluteUri(cachedPath);
            if (string.IsNullOrWhiteSpace(cachedUri))
            {
                return;
            }

            var cachedImage = CreateCoverImage(cachedUri, sourceUrl, steamAppId, targetWidth, targetHeight);
            var coverImage = cachedImage;
            var coverSource = cachedUri;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
            {
                if (operationId != Volatile.Read(ref _coverLoadOperationId))
                {
                    return;
                }

                ApplyCoverImageIfNeeded(coverSource, coverImage, targetWidth, targetHeight);
                return;
            }

            await dispatcher.InvokeAsync(() =>
            {
                if (operationId != Volatile.Read(ref _coverLoadOperationId))
                {
                    return;
                }

                ApplyCoverImageIfNeeded(coverSource, coverImage, targetWidth, targetHeight);
            });
        }
        catch
        {
            // Ignore cover image prefetch failure.
        }
        finally
        {
            lock (_coverLoadGate)
            {
                if (string.Equals(_activeCoverLoadKey, loadKey, StringComparison.OrdinalIgnoreCase))
                {
                    _activeCoverLoadKey = "";
                }
            }
        }
    }

    private bool ApplyCoverImageIfNeeded(
        string source,
        ImageSource image,
        double targetWidth,
        double targetHeight)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source)
            ? DefaultCoverImageSource
            : source.Trim();

        if (IsCurrentCoverImageRequest(normalizedSource, targetWidth, targetHeight))
        {
            Debug.WriteLine($"home_cover_reload skipped reason=same_source game={ResolveLogGameId()}");
            return false;
        }

        return ApplyCoverImage(normalizedSource, image, targetWidth, targetHeight);
    }

    private bool ApplyCoverImage(
        string source,
        ImageSource image,
        double targetWidth,
        double targetHeight)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source)
            ? DefaultCoverImageSource
            : source.Trim();

        CoverImageSource = normalizedSource;
        CoverImage = image;
        _lastAppliedCoverSource = normalizedSource;
        _lastAppliedCoverWidth = targetWidth;
        _lastAppliedCoverHeight = targetHeight;

        var reloadCount = Interlocked.Increment(ref CoverReloadCount);
        Debug.WriteLine($"[Cover] reload={reloadCount} path={normalizedSource}");
        Debug.WriteLine($"home_cover_reload applied game={ResolveLogGameId()}");
        return true;
    }

    private bool IsCurrentCoverImageRequest(string source, double targetWidth, double targetHeight)
    {
        return string.Equals(_lastAppliedCoverSource, source, StringComparison.OrdinalIgnoreCase)
               && Math.Abs(_lastAppliedCoverWidth - targetWidth) < CoverImageWidthRefreshThresholdDip
               && Math.Abs(_lastAppliedCoverHeight - targetHeight) < CoverImageHeightRefreshThresholdDip;
    }

    private static bool IsDefaultCoverSource(string source)
    {
        return string.Equals(
            (source ?? "").Trim(),
            DefaultCoverImageSource,
            StringComparison.OrdinalIgnoreCase);
    }

    private ImageSource CreateCoverImage(
        string source,
        string sourceUrl = "",
        string steamAppId = "",
        double? targetWidth = null,
        double? targetHeight = null)
    {
        return DpiAwareCoverImageSourceService.CreateOrDefault(
            source,
            targetWidth ?? CardWidth,
            targetHeight ?? CardHeight,
            Application.Current?.MainWindow,
            sourceUrlForCache: string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl,
            steamAppId: string.IsNullOrWhiteSpace(steamAppId) ? null : steamAppId);
    }

    private string ResolveLogGameId()
    {
        var sourceGameId = (SourceModel?.GameId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(sourceGameId))
        {
            return sourceGameId;
        }

        return (GameEntry.GameId ?? "").Trim();
    }

    private static string BuildCoverLoadKey(string sourceUrl, string steamAppId)
    {
        var normalizedSteamAppId = (steamAppId ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalizedSteamAppId)
            ? $"url::{(sourceUrl ?? "").Trim()}"
            : $"steam::{normalizedSteamAppId}";
    }

    private static ImageSource CreateDefaultCoverImage()
    {
        return DpiAwareCoverImageSourceService.CreateOrDefault(
            DefaultCoverImageSource,
            0,
            0,
            defaultCoverSource: DefaultCoverImageSource,
            allowOriginalCacheFallback: false);
    }

    private static string? SafeAbsoluteUri(string path)
    {
        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.AbsoluteUri;
    }
}

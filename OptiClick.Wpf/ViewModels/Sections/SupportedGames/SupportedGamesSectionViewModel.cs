using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Wiki;

namespace OptiClick.Wpf.ViewModels.Sections.SupportedGames;

public sealed class SupportedGamesSectionViewModel : ViewModelBase
{
    private const int VisibleCoverBufferRows = 1;
    private const int VisibleCoverFallbackRows = 1;
    private const int CoverLoadMaxDegreeOfParallelism = 2;
    private const double RowBottomMarginDip = 8.0;

    private readonly ISupportedGamesWikiMarkdownLoader _markdownLoader;
    private readonly StartupBackgroundTaskManager _backgroundTaskManager;
    private readonly IAppLogger _logger;
    private readonly Func<AppLanguage> _languageAccessor;
    private readonly Func<AppStrings> _stringsAccessor;
    private readonly Func<bool> _isActiveAccessor;
    private readonly Func<ICommand> _openGameSupportRequestCommandAccessor;
    private readonly Action<Func<StartupPreparationState, StartupPreparationState>> _updateStartupPreparationState;
    private readonly object _coverLoadGate = new();
    private readonly HashSet<string> _coverLoadRequestedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _coverLoadLimiter = new(CoverLoadMaxDegreeOfParallelism, CoverLoadMaxDegreeOfParallelism);
    private IReadOnlyList<SupportedGamesWikiEntry> _entries = [];
    private IReadOnlyList<SupportedGamesWikiRowViewModel> _allRows = [];
    private string _searchText = "";
    private string _statusText = "";
    private bool _isLoading;
    private bool _isCatalogLoadedForView;
    private bool _isCacheRefreshRunning;
    private bool _hasPendingCatalogRefresh;

    private enum CoverLoadOutcome
    {
        Skipped,
        Updated,
        Failed
    }

    private sealed record CoverLoadResult(CoverLoadOutcome Outcome, string Reason = "");

    public SupportedGamesSectionViewModel(
        ISupportedGamesWikiMarkdownLoader markdownLoader,
        StartupBackgroundTaskManager backgroundTaskManager,
        IAppLogger logger,
        Func<AppLanguage> languageAccessor,
        Func<AppStrings> stringsAccessor,
        Func<bool> isActiveAccessor,
        Func<ICommand> openGameSupportRequestCommandAccessor,
        Action<Func<StartupPreparationState, StartupPreparationState>> updateStartupPreparationState)
    {
        _markdownLoader = markdownLoader ?? throw new ArgumentNullException(nameof(markdownLoader));
        _backgroundTaskManager = backgroundTaskManager ?? throw new ArgumentNullException(nameof(backgroundTaskManager));
        _logger = logger ?? NullAppLogger.Instance;
        _languageAccessor = languageAccessor ?? throw new ArgumentNullException(nameof(languageAccessor));
        _stringsAccessor = stringsAccessor ?? throw new ArgumentNullException(nameof(stringsAccessor));
        _isActiveAccessor = isActiveAccessor ?? throw new ArgumentNullException(nameof(isActiveAccessor));
        _openGameSupportRequestCommandAccessor = openGameSupportRequestCommandAccessor ?? throw new ArgumentNullException(nameof(openGameSupportRequestCommandAccessor));
        _updateStartupPreparationState = updateStartupPreparationState ?? throw new ArgumentNullException(nameof(updateStartupPreparationState));
        SupportedGamesWikiRows = new ObservableCollection<SupportedGamesWikiRowViewModel>();
        ClearSupportedGamesWikiSearchCommand = new RelayCommand(
            _ => ClearSupportedGamesWikiSearch(),
            _ => HasSupportedGamesWikiSearchText);
    }

    public ObservableCollection<SupportedGamesWikiRowViewModel> SupportedGamesWikiRows { get; }

    public bool HasEntries => _entries.Count > 0;

    public string SupportedGamesWikiSearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value ?? ""))
            {
                return;
            }

            if (_isCatalogLoadedForView)
            {
                ApplyFilter();
            }

            OnPropertyChanged(nameof(HasSupportedGamesWikiSearchText));
            ClearSupportedGamesWikiSearchCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasSupportedGamesWikiSearchText => !string.IsNullOrWhiteSpace(SupportedGamesWikiSearchText);

    public string SupportedGamesWikiStatusText
    {
        get => _statusText;
        private set
        {
            if (!SetProperty(ref _statusText, value ?? ""))
            {
                return;
            }

            OnPropertyChanged(nameof(SupportedGamesWikiStatusVisibility));
            OnPropertyChanged(nameof(SupportedGamesWikiEmptyVisibility));
        }
    }

    public bool IsSupportedGamesWikiLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SupportedGamesWikiLoadingVisibility));
            OnPropertyChanged(nameof(SupportedGamesWikiEmptyVisibility));
        }
    }

    public Visibility SupportedGamesWikiStatusVisibility =>
        string.IsNullOrWhiteSpace(SupportedGamesWikiStatusText) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SupportedGamesWikiLoadingVisibility =>
        IsSupportedGamesWikiLoading ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SupportedGamesWikiEmptyVisibility =>
        !IsSupportedGamesWikiLoading
        && string.IsNullOrWhiteSpace(SupportedGamesWikiStatusText)
        && SupportedGamesWikiRows.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string SupportedGamesWikiEmptyText =>
        Language == AppLanguage.Korean
            ? "\uAC80\uC0C9 \uC870\uAC74\uC5D0 \uB9DE\uB294 \uC9C0\uC6D0 \uAC8C\uC784\uC774 \uC5C6\uC2B5\uB2C8\uB2E4."
            : "No supported games match the current search.";

    public AppStrings Strings => _stringsAccessor();

    public ICommand OpenGameSupportRequestCommand => _openGameSupportRequestCommandAccessor();

    public RelayCommand ClearSupportedGamesWikiSearchCommand { get; }

    private AppLanguage Language => _languageAccessor();

    private void ClearSupportedGamesWikiSearch()
    {
        SupportedGamesWikiSearchText = "";
    }

    public void LoadFromCache()
    {
        EnsureLoadedForView();
    }

    public void EnsureLoadedForView()
    {
        try
        {
            if (_isCatalogLoadedForView && !_hasPendingCatalogRefresh)
            {
                LogInfo("supported_games_view_load skipped reason=already_loaded");
                return;
            }

            LogInfo("supported_games_view_load started source=entries_cache");
            IsSupportedGamesWikiLoading = true;
            var cachedEntries = _markdownLoader.LoadCachedOrEmpty();
            SetEntriesForView(cachedEntries, "entries_cache");
            _isCatalogLoadedForView = true;
            _hasPendingCatalogRefresh = false;
            if (_entries.Count > 0)
            {
                SetStatusText("");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"supported games wiki cache load failed type={ex.GetType().Name}");
            SetStatusText(GetLoadFailureText());
        }
        finally
        {
            IsSupportedGamesWikiLoading = false;
        }
    }

    public void StartRefreshInBackground()
    {
        if (_isCacheRefreshRunning)
        {
            LogInfo("supported_games_cache_refresh skipped reason=already_running");
            return;
        }

        _isCacheRefreshRunning = true;
        _updateStartupPreparationState(state => state with
        {
            SupportedGamesWikiRefreshStarted = true,
            SupportedGamesWikiRefreshRunning = true,
            SupportedGamesWikiRefreshCompleted = false,
            SupportedGamesWikiRefreshCanceled = false,
            SupportedGamesWikiRefreshFailed = false
        });
        var cancellationTokenSource = _backgroundTaskManager.CreateSource();
        _ = RefreshWithLogGuardAsync(cancellationTokenSource);
    }

    public void RefreshAfterLanguageChange()
    {
        if (_isCatalogLoadedForView)
        {
            RebuildRows();
        }

        RefreshStatusTextForLanguage();
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(SupportedGamesWikiEmptyText));
    }

    public void RebuildRows()
    {
        if (!_isCatalogLoadedForView)
        {
            return;
        }

        var wikiRowWidthDip = SupportedGamesWikiLayoutProfile.ResolveWikiRowCoverWidthDip();
        var wikiRowHeightDip = SupportedGamesWikiLayoutProfile.ResolveWikiRowCoverHeightDip();
        lock (_coverLoadGate)
        {
            _coverLoadRequestedKeys.Clear();
        }

        _allRows = _entries
            .Select(entry =>
            {
                var displayTitle = ResolveWikiDisplayTitle(entry, Language);
                var (coverLookupUrl, steamAppId) = ResolveSupportedGamesWikiCoverLookup(entry);
                return new SupportedGamesWikiRowViewModel
                {
                    GameId = (entry.GameId ?? "").Trim(),
                    DisplayTitle = displayTitle,
                    GameNameEn = (entry.GameNameEn ?? "").Trim(),
                    GameNameKr = (entry.GameNameKr ?? "").Trim(),
                    IntelText = NormalizeVendorText(entry.IntelText),
                    AmdText = NormalizeVendorText(entry.AmdText),
                    NvidiaText = NormalizeVendorText(entry.NvidiaText),
                    CoverLookupUrl = coverLookupUrl,
                    CoverSteamAppId = steamAppId,
                    CoverImageSource = CoverImageCacheService.DefaultCoverImageSource,
                    CoverImageWidthDip = wikiRowWidthDip,
                    CoverImageHeightDip = wikiRowHeightDip
                };
            })
            .OrderBy(static row => row.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ApplyFilter();
    }

    public void QueueVisibleCoverLoad(double verticalOffset, double viewportHeight)
    {
        if (SupportedGamesWikiRows.Count == 0)
        {
            return;
        }

        var visibleRows = ResolveVisibleRows(verticalOffset, viewportHeight);
        var targets = new List<SupportedGamesWikiRowViewModel>(visibleRows.Count);
        lock (_coverLoadGate)
        {
            foreach (var row in visibleRows)
            {
                if (!ShouldQueueCoverLoad(row))
                {
                    continue;
                }

                var requestKey = BuildCoverLoadRequestKey(row);
                if (string.IsNullOrWhiteSpace(requestKey) || !_coverLoadRequestedKeys.Add(requestKey))
                {
                    continue;
                }

                targets.Add(row);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        LogInfo($"supported_games_visible_cover_load queued total={targets.Count} offset={Math.Round(verticalOffset, 2)} viewport={Math.Round(viewportHeight, 2)}");
        var cancellationTokenSource = _backgroundTaskManager.CreateSource();
        _ = RunVisibleCoverLoadAsync(targets, cancellationTokenSource);
    }

    internal static string ResolveSupportedGamesWikiCoverSource(
        SupportedGamesWikiEntry entry,
        string defaultCoverSource = CoverImageCacheService.DefaultCoverImageSource)
    {
        return ResolveSupportedGamesWikiCoverSourceWithLookup(entry, defaultCoverSource).coverSource;
    }

    internal static (string coverSource, string coverLookupUrl, string steamAppId) ResolveSupportedGamesWikiCoverSourceWithLookup(
        SupportedGamesWikiEntry entry,
        string defaultCoverSource = CoverImageCacheService.DefaultCoverImageSource)
    {
        var (coverLookupUrl, steamAppId) = ResolveSupportedGamesWikiCoverLookup(entry);

        return (
            CoverImageCacheService.ResolveLocalCoverSourceOrDefault(
                coverLookupUrl,
                steamAppId,
                defaultCoverSource),
            coverLookupUrl,
            steamAppId);
    }

    private static (string coverLookupUrl, string steamAppId) ResolveSupportedGamesWikiCoverLookup(
        SupportedGamesWikiEntry entry)
    {
        var coverUrl = (entry.CoverUrl ?? "").Trim();
        var steamAppId = (entry.CoverSteamAppId ?? "").Trim();
        var coverLookupUrl = IsHttpUrl(coverUrl)
            ? coverUrl
            : string.IsNullOrWhiteSpace(steamAppId)
                ? ""
                : CoverImageCacheService.BuildSteamCoverUrl(steamAppId);

        return (coverLookupUrl, steamAppId);
    }

    internal IReadOnlyList<SupportedGamesWikiRowViewModel> ResolveVisibleRows(
        double verticalOffset,
        double viewportHeight)
    {
        var rowCount = SupportedGamesWikiRows.Count;
        if (rowCount == 0)
        {
            return [];
        }

        var rowPitch = Math.Max(
            1.0,
            SupportedGamesWikiLayoutProfile.ResolveRowHeightDip() + RowBottomMarginDip);
        var safeOffset = double.IsNaN(verticalOffset) || double.IsInfinity(verticalOffset)
            ? 0
            : Math.Max(0, verticalOffset);
        var visibleCount = double.IsNaN(viewportHeight) || double.IsInfinity(viewportHeight) || viewportHeight <= 0
            ? VisibleCoverFallbackRows
            : Math.Max(1, (int)Math.Ceiling(viewportHeight / rowPitch));

        var firstVisibleIndex = Math.Clamp((int)Math.Floor(safeOffset / rowPitch), 0, rowCount - 1);
        var startIndex = Math.Max(0, firstVisibleIndex - VisibleCoverBufferRows);
        var requestedCount = visibleCount + VisibleCoverBufferRows * 2;
        var endIndex = Math.Min(rowCount, startIndex + requestedCount);
        var rows = new List<SupportedGamesWikiRowViewModel>(Math.Max(0, endIndex - startIndex));
        for (var index = startIndex; index < endIndex; index++)
        {
            rows.Add(SupportedGamesWikiRows[index]);
        }

        return rows;
    }

    private async Task RefreshWithLogGuardAsync(CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var canceled = false;
        var failed = false;
        try
        {
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            canceled = true;
            LogWarning("supported games wiki refresh background skipped reason=canceled");
        }
        catch (Exception ex)
        {
            failed = true;
            LogWarning($"supported games wiki refresh background failed type={ex.GetType().Name}");
        }
        finally
        {
            _isCacheRefreshRunning = false;
            _updateStartupPreparationState(state => state with
            {
                SupportedGamesWikiRefreshRunning = false,
                SupportedGamesWikiRefreshCompleted = !canceled && !failed,
                SupportedGamesWikiRefreshCanceled = canceled,
                SupportedGamesWikiRefreshFailed = failed,
                LastErrorCode = failed
                    ? "supported_games_wiki_refresh_failed"
                    : !canceled
                        ? ClearLastErrorCode(state.LastErrorCode, "supported_games_wiki_refresh_failed")
                        : state.LastErrorCode
            });
            _backgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        SupportedGamesWikiMarkdownRefreshResult result;
        result = await _markdownLoader.RefreshAsync(cancellationToken);

        if (!result.DidRun)
        {
            LogInfo($"supported_games_cache_refresh skipped reason={NormalizeStatusCode(result.ErrorCode, "not_run")}");
            return;
        }

        if (!result.IsSuccess)
        {
            var code = NormalizeStatusCode(result.ErrorCode, MainViewModelStatusCodes.Unknown);
            LogWarning($"supported games wiki refresh failed code={code}");
            if (_isActiveAccessor() && _entries.Count == 0)
            {
                SetStatusText(GetLoadFailureText());
            }

            return;
        }

        if (!result.DidUpdate)
        {
            LogInfo("supported_games_cache_refresh skipped reason=not_modified");
            return;
        }

        if (_isActiveAccessor())
        {
            SetStatusText("");
            SetEntriesForView(_markdownLoader.LoadCachedOrEmpty(), "remote_refresh");
            _isCatalogLoadedForView = true;
            _hasPendingCatalogRefresh = false;
            LogInfo($"supported games wiki refresh success games={_entries.Count}");
            return;
        }

        if (_isCatalogLoadedForView)
        {
            _hasPendingCatalogRefresh = true;
        }

        LogInfo($"supported games wiki cache refresh success entries={result.Entries.Count}");
    }

    private void SetEntriesForView(IReadOnlyList<SupportedGamesWikiEntry>? entries, string source)
    {
        var safeEntries = entries ?? [];
        _entries = safeEntries
            .Where(static entry => entry is not null)
            .ToArray();
        _isCatalogLoadedForView = true;
        RebuildRows();
        LogInfo($"supported_games_view_load completed source={NormalizeStatusCode(source, "unknown")} rows={SupportedGamesWikiRows.Count}");
    }

    private void ApplyFilter()
    {
        var query = (_searchText ?? "").Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allRows
            : _allRows
                .Where(row => MatchesSearch(row, query))
                .ToArray();

        SupportedGamesWikiRows.Clear();
        foreach (var row in filtered)
        {
            SupportedGamesWikiRows.Add(row);
        }

        OnPropertyChanged(nameof(SupportedGamesWikiEmptyVisibility));
        if (_isActiveAccessor())
        {
            QueueVisibleCoverLoad(0, 0);
        }
    }

    private async Task RunVisibleCoverLoadAsync(
        IReadOnlyList<SupportedGamesWikiRowViewModel> rows,
        CancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        try
        {
            var tasks = rows.Select(row => LoadCoverAsync(row, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var updatedCount = results.Count(static result => result.Outcome == CoverLoadOutcome.Updated);
            var skippedCount = results.Count(static result => result.Outcome == CoverLoadOutcome.Skipped);
            var failedCount = results.Count(static result => result.Outcome == CoverLoadOutcome.Failed);
            var failureReasons = BuildCoverLoadFailureSummary(results);
            var failureText = string.IsNullOrWhiteSpace(failureReasons)
                ? ""
                : $" failure_reasons={failureReasons}";
            LogInfo($"supported_games_visible_cover_load completed total={rows.Count} updated={updatedCount} skipped={skippedCount} failed={failedCount}{failureText}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogWarning("supported games visible cover load skipped reason=canceled");
        }
        catch (Exception ex)
        {
            LogWarning($"supported games visible cover load failed type={ex.GetType().Name}");
        }
        finally
        {
            _backgroundTaskManager.Remove(cancellationTokenSource);
        }
    }

    private async Task<CoverLoadResult> LoadCoverAsync(
        SupportedGamesWikiRowViewModel row,
        CancellationToken cancellationToken)
    {
        await _coverLoadLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        var result = new CoverLoadResult(CoverLoadOutcome.Failed, "unknown");
        try
        {
            if (!ShouldQueueCoverLoad(row))
            {
                result = new CoverLoadResult(CoverLoadOutcome.Skipped, "not_queueable");
                return result;
            }

            var cachedPath = await CoverImageCacheService.EnsureCachedAsync(
                row.CoverLookupUrl,
                row.CoverSteamAppId,
                cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                result = new CoverLoadResult(CoverLoadOutcome.Failed, "cache_miss");
                return result;
            }

            var cachedUri = SafeAbsoluteUri(cachedPath);
            if (string.IsNullOrWhiteSpace(cachedUri))
            {
                result = new CoverLoadResult(CoverLoadOutcome.Failed, "invalid_cache_uri");
                return result;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted)
            {
                var image = CreateCoverImage(row, cachedUri);
                result = ApplyCoverToMatchingRows(row, cachedUri, image)
                    ? new CoverLoadResult(CoverLoadOutcome.Updated)
                    : new CoverLoadResult(CoverLoadOutcome.Failed, "apply_failed");
                return result;
            }

            var applied = await dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var image = CreateCoverImage(row, cachedUri);
                return ApplyCoverToMatchingRows(row, cachedUri, image);
            });

            result = applied
                ? new CoverLoadResult(CoverLoadOutcome.Updated)
                : new CoverLoadResult(CoverLoadOutcome.Failed, "apply_failed");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = new CoverLoadResult(CoverLoadOutcome.Failed, ex.GetType().Name);
            return result;
        }
        finally
        {
            if (result.Outcome == CoverLoadOutcome.Failed)
            {
                ForgetCoverLoadRequest(row);
            }

            _coverLoadLimiter.Release();
        }
    }

    private ImageSource CreateCoverImage(SupportedGamesWikiRowViewModel row, string cachedUri)
    {
        return DpiAwareCoverImageSourceService.CreateOrDefault(
            cachedUri,
            row.CoverImageWidthDip,
            row.CoverImageHeightDip,
            Application.Current?.MainWindow,
            sourceUrlForCache: row.CoverLookupUrl,
            steamAppId: row.CoverSteamAppId,
            allowOriginalCacheFallback: true);
    }

    private bool ApplyCoverToMatchingRows(
        SupportedGamesWikiRowViewModel sourceRow,
        string cachedUri,
        ImageSource image)
    {
        var requestKey = BuildCoverLoadRequestKey(sourceRow);
        if (string.IsNullOrWhiteSpace(requestKey))
        {
            return false;
        }

        var updated = false;
        foreach (var row in _allRows)
        {
            if (!string.Equals(BuildCoverLoadRequestKey(row), requestKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            row.CoverImageSource = cachedUri;
            row.CoverImage = image;
            updated = true;
        }

        return updated;
    }

    private void ForgetCoverLoadRequest(SupportedGamesWikiRowViewModel row)
    {
        var requestKey = BuildCoverLoadRequestKey(row);
        if (string.IsNullOrWhiteSpace(requestKey))
        {
            return;
        }

        lock (_coverLoadGate)
        {
            _coverLoadRequestedKeys.Remove(requestKey);
        }
    }

    private static string BuildCoverLoadFailureSummary(IReadOnlyList<CoverLoadResult> results)
    {
        var summaries = results
            .Where(static result => result.Outcome == CoverLoadOutcome.Failed)
            .Select(static result => NormalizeStatusCode(result.Reason, "unknown"))
            .GroupBy(static reason => reason, StringComparer.OrdinalIgnoreCase)
            .Select(static group => $"{group.Key}:{group.Count()}")
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return summaries.Length == 0
            ? ""
            : string.Join(",", summaries);
    }

    private static bool ShouldQueueCoverLoad(SupportedGamesWikiRowViewModel row)
    {
        if (!IsHttpUrl(row.CoverLookupUrl))
        {
            return false;
        }

        var currentSource = (row.CoverImageSource ?? "").Trim();
        return string.IsNullOrWhiteSpace(currentSource)
               || string.Equals(currentSource, CoverImageCacheService.DefaultCoverImageSource, StringComparison.OrdinalIgnoreCase)
               || currentSource.StartsWith("pack://application:", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCoverLoadRequestKey(SupportedGamesWikiRowViewModel row)
    {
        return BuildCoverTargetKey(row.CoverLookupUrl, row.CoverSteamAppId);
    }

    private static string BuildCoverTargetKey(string sourceUrl, string steamAppId)
    {
        var normalizedSteamAppId = (steamAppId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSteamAppId))
        {
            return $"steam::{normalizedSteamAppId}";
        }

        var normalizedSourceUrl = (sourceUrl ?? "").Trim();
        return IsHttpUrl(normalizedSourceUrl)
            ? $"url::{normalizedSourceUrl}"
            : "";
    }

    private static string? SafeAbsoluteUri(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uri)
            ? uri.AbsoluteUri
            : null;
    }

    private string GetLoadFailureText()
    {
        return Language == AppLanguage.Korean
            ? "지원 게임 목록을 불러오지 못했습니다. 잠시 후 다시 시도해주세요."
            : "Failed to load the supported games list. Please try again later.";
    }

    private void SetStatusText(string value)
    {
        SupportedGamesWikiStatusText = (value ?? "").Trim();
    }

    private void RefreshStatusTextForLanguage()
    {
        if (string.IsNullOrWhiteSpace(SupportedGamesWikiStatusText))
        {
            return;
        }

        SetStatusText(GetLoadFailureText());
    }

    private static string ResolveWikiDisplayTitle(SupportedGamesWikiEntry entry, AppLanguage language)
    {
        var nameEn = (entry.GameNameEn ?? "").Trim();
        var nameKr = (entry.GameNameKr ?? "").Trim();
        var gameId = (entry.GameId ?? "").Trim();

        if (language == AppLanguage.Korean)
        {
            if (!string.IsNullOrWhiteSpace(nameKr))
            {
                return nameKr;
            }

            if (!string.IsNullOrWhiteSpace(nameEn))
            {
                return nameEn;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(nameEn))
            {
                return nameEn;
            }

            if (!string.IsNullOrWhiteSpace(nameKr))
            {
                return nameKr;
            }
        }

        return gameId;
    }

    private static bool MatchesSearch(SupportedGamesWikiRowViewModel row, string query)
    {
        var normalizedQuery = (query ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return ContainsInvariant(row.DisplayTitle, normalizedQuery)
               || ContainsInvariant(row.GameNameEn, normalizedQuery)
               || ContainsInvariant(row.GameNameKr, normalizedQuery)
               || ContainsInvariant(row.GameId, normalizedQuery);
    }

    private static bool ContainsInvariant(string source, string query)
    {
        return (source ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeVendorText(string value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
    }

    private static string NormalizeStatusCode(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string ClearLastErrorCode(string lastErrorCode, string errorCode)
    {
        return string.Equals(lastErrorCode, errorCode, StringComparison.OrdinalIgnoreCase)
            ? ""
            : lastErrorCode;
    }

    private void LogInfo(string message)
    {
        _logger.Info(MainViewModelLogCategories.Wiki, message);
    }

    private void LogWarning(string message)
    {
        _logger.Warning(MainViewModelLogCategories.Wiki, message);
    }
}

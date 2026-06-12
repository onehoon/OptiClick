using System;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Core.Models;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainLanguageChangeController
{
    public async Task ApplyAsync(
        MainLanguageChangeContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            context.Services.SetLanguage(context.Language);
            context.Callbacks.LogInfo($"ui_language_changed source=settings value={context.Services.ToLanguageCode(context.Language)}");
            context.Services.RefreshLocalizedStrings();
            context.Services.ApplySelectedGameLocalization();
            context.Services.RefreshSupportedGamesAfterLanguageChange();
            var localizedStrings = context.Services.ReadStrings();
            context.Callbacks.ApplyLocalizationStateUpdate(context.Services.BuildRefreshState(context.Language, localizedStrings));
            await context.Services.RefreshRuntimeContextAsync(cancellationToken);
            var refreshedVisibleGames = await context.Services.RefreshVisibleGamesAfterLanguageChangeAsync(cancellationToken);
            if (!refreshedVisibleGames)
            {
                await context.Services.RecomputeSelectionAfterScanAsync(cancellationToken, false);
            }
        }
        catch (Exception ex)
        {
            context.Callbacks.LogWarning("language change refresh failed", ex);
        }
    }
}

internal sealed class MainLanguageChangeContextFactory
{
    private readonly MainLanguageChangeServices _services;
    private readonly MainLanguageChangeCallbacks _callbacks;

    public MainLanguageChangeContextFactory(MainLanguageChangeContextFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _services = new MainLanguageChangeServices
        {
            SetLanguage = input.SetLanguage,
            RefreshLocalizedStrings = input.RefreshLocalizedStrings,
            ApplySelectedGameLocalization = input.ApplySelectedGameLocalization,
            RefreshSupportedGamesAfterLanguageChange = input.RefreshSupportedGamesAfterLanguageChange,
            BuildRefreshState = input.BuildRefreshState,
            RefreshRuntimeContextAsync = input.RefreshRuntimeContextAsync,
            RecomputeSelectionAfterScanAsync = input.RecomputeSelectionAfterScanAsync,
            RefreshVisibleGamesAfterLanguageChangeAsync = input.RefreshVisibleGamesAfterLanguageChangeAsync,
            ToLanguageCode = ToLanguageCode,
            ReadStrings = input.ReadStrings
        };
        _callbacks = new MainLanguageChangeCallbacks
        {
            LogInfo = input.LogInfo,
            LogWarning = input.LogWarning,
            ApplyLocalizationStateUpdate = input.ApplyLocalizationStateUpdate
        };
    }

    public MainLanguageChangeContext Create(AppLanguage language)
    {
        return new MainLanguageChangeContext
        {
            Language = language,
            Services = _services,
            Callbacks = _callbacks
        };
    }

    private static string ToLanguageCode(AppLanguage selectedLanguage)
    {
        return selectedLanguage == AppLanguage.Korean ? "ko" : "en";
    }
}

internal sealed record MainLanguageChangeContextFactoryInput
{
    public required Action<AppLanguage> SetLanguage { get; init; }
    public required Action RefreshLocalizedStrings { get; init; }
    public required Action ApplySelectedGameLocalization { get; init; }
    public required Action RefreshSupportedGamesAfterLanguageChange { get; init; }
    public required Func<AppLanguage, AppStrings, LocalizationStateUpdate> BuildRefreshState { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<CancellationToken, Task<bool>> RefreshVisibleGamesAfterLanguageChangeAsync { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string, Exception> LogWarning { get; init; }
    public required Action<LocalizationStateUpdate> ApplyLocalizationStateUpdate { get; init; }
}

internal sealed class MainLanguageChangeContext
{
    public required AppLanguage Language { get; init; }
    public required MainLanguageChangeServices Services { get; init; }
    public required MainLanguageChangeCallbacks Callbacks { get; init; }
}

internal sealed class MainLanguageChangeServices
{
    public required Action<AppLanguage> SetLanguage { get; init; }
    public required Action RefreshLocalizedStrings { get; init; }
    public required Action ApplySelectedGameLocalization { get; init; }
    public required Action RefreshSupportedGamesAfterLanguageChange { get; init; }
    public required Func<AppLanguage, AppStrings, LocalizationStateUpdate> BuildRefreshState { get; init; }
    public required Func<CancellationToken, Task> RefreshRuntimeContextAsync { get; init; }
    public required Func<CancellationToken, bool, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Func<CancellationToken, Task<bool>> RefreshVisibleGamesAfterLanguageChangeAsync { get; init; }
    public required Func<AppLanguage, string> ToLanguageCode { get; init; }
    public required Func<AppStrings> ReadStrings { get; init; }
}

internal sealed class MainLanguageChangeCallbacks
{
    public required Action<string> LogInfo { get; init; }
    public required Action<string, Exception> LogWarning { get; init; }
    public required Action<LocalizationStateUpdate> ApplyLocalizationStateUpdate { get; init; }
}

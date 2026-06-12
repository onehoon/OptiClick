using System.Collections.Generic;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class StartupPreparationContextFactory
{
    private readonly StartupPreparationContextFactoryInput _input;

    public StartupPreparationContextFactory(StartupPreparationContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public StartupPreparationCoordinatorRequest CreateRequest()
    {
        return new StartupPreparationCoordinatorRequest
        {
            StatePort = new StartupPreparationStatePort
            {
                ReadLatestArchiveReadiness = _input.ReadLatestArchiveReadiness,
                SetArchiveReadiness = _input.SetArchiveReadiness,
                UpdateStartupPreparationState = _input.UpdateStartupPreparationState,
                ClearLastErrorCode = _input.ClearLastErrorCode
            },
            UiPort = new StartupPreparationUiPort
            {
                ApplyFirstRunPreparationOverlay = _input.ApplyFirstRunPreparationOverlay,
                ShowFirstRunPreparationFailureAsync =
                    _input.ShowFirstRunPreparationFailureAsync
            },
            RuntimePort = new StartupPreparationRuntimePort
            {
                ShouldBlockStartupForUnsupportedOperatingSystem =
                    _input.ShouldBlockStartupForUnsupportedOperatingSystem,
                ReadModuleDownloadLinks = _input.ReadModuleDownloadLinks,
                ReadFsr4VariantCatalog = _input.ReadFsr4VariantCatalog,
                RefreshArchiveReadinessWithoutCoordinatorAsync =
                    _input.RefreshArchiveReadinessWithoutCoordinatorAsync,
                RecomputeSelectionAfterScanAsync = _input.RecomputeSelectionAfterScanAsync
            },
            LogPort = new StartupPreparationLogPort
            {
                LogAppInfo = _input.LogAppInfo,
                LogAppWarning = _input.LogAppWarning,
                LogInstallInfo = _input.LogInstallInfo,
                LogInstallWarning = _input.LogInstallWarning
            },
            FirstRunPreparationFailureText = new StartupPreparationFailureText
            {
                Title = _input.ReadFirstRunPreparationFailedTitle(),
                Summary = _input.ReadFirstRunPreparationFailedSummary(),
                PrimaryButtonText = _input.ReadDialogButtonOkText()
            }
        };
    }
}

internal sealed record StartupPreparationContextFactoryInput
{
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required Action<ArchiveReadinessSnapshot> SetArchiveReadiness { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState
    {
        get;
        init;
    }

    public required Func<string, string, string> ClearLastErrorCode { get; init; }
    public required Action<bool> ApplyFirstRunPreparationOverlay { get; init; }
    public required Func<AppDialogRequest, CancellationToken, Task> ShowFirstRunPreparationFailureAsync { get; init; }
    public required Func<bool> ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Func<Fsr4VariantCatalog> ReadFsr4VariantCatalog { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessWithoutCoordinatorAsync
    {
        get;
        init;
    }

    public required Func<CancellationToken, Task> RecomputeSelectionAfterScanAsync { get; init; }
    public required Action<string> LogAppInfo { get; init; }
    public required Action<string> LogAppWarning { get; init; }
    public required Action<string> LogInstallInfo { get; init; }
    public required Action<string> LogInstallWarning { get; init; }
    public required Func<string> ReadFirstRunPreparationFailedTitle { get; init; }
    public required Func<string> ReadFirstRunPreparationFailedSummary { get; init; }
    public required Func<string> ReadDialogButtonOkText { get; init; }
}

using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Windows;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.ViewModels.Features.Runtime;

namespace OptiClick.Wpf.ViewModels.Ports.App;

internal sealed record MainShellAppPortCompositionInput
{
    public required MainAppResolvedDependencies AppDependencies { get; init; }
    public required MainShellResolvedDependencies ShellDependencies { get; init; }
    public required MainStartupResolvedDependencies StartupDependencies { get; init; }
    public required IMainShellAppPortAccess Access { get; init; }
    public required Func<MainRuntimeFeatureFacade> ResolveRuntimeFeature { get; init; }
    public IWindowsWmiServiceRecovery? WmiServiceRecovery { get; init; }
}

internal static class MainShellAppPortComposer
{
    public static MainShellFacadeAppPort Compose(MainShellAppPortCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var appDependencies = input.AppDependencies;
        var shellDependencies = input.ShellDependencies;
        var startupDependencies = input.StartupDependencies;
        var access = input.Access;
        var wmiServiceRecovery = input.WmiServiceRecovery
                                 ?? new WindowsWmiServiceRecovery(appDependencies.AppLogger);

        return new MainShellFacadeAppPort
        {
            LocalDataPathProvider = appDependencies.LocalDataPathProvider,
            AppLogger = appDependencies.AppLogger,
            ReadStrings = () => access.Strings,
            DialogPresenter = shellDependencies.DialogPresenter,
            FlowLogDispatcher = shellDependencies.FlowLogDispatcher,
            ResultApplier = shellDependencies.ResultApplier,
            RemoteCatalogDialogGate = shellDependencies.RemoteCatalogDialogGate,
            FlowRequestFactory = shellDependencies.FlowRequestFactory,
            OperationLocks = access.OperationLocks,
            InstallManagementDialogService = shellDependencies.InstallManagementDialogService,
            ExternalUrlLauncher = appDependencies.ExternalUrlLauncher,
            ReadAppVersion = () => NormalizeAppVersion(appDependencies.AppVersionProvider.GetCurrentVersion()),
            IsKoreanUi = () => access.SelectedLanguage == AppLanguage.Korean,
            SetSettingsStatusText = access.SetSettingsStatusText,
            SetScanStatusText = access.SetScanStatusText,
            ApplyStateUpdate = access.ApplyStateUpdate,
            ApplyDeferredStateUpdate = access.ApplyDeferredStateUpdate,
            ShouldBlockStartupForUnsupportedOperatingSystem =
                () => input.ResolveRuntimeFeature().IsUnsupportedOperatingSystem(),
            ShowRemoteCatalogDialogOnceAsync = (request, ct) =>
                startupDependencies.MainStartupDialogsController.ShowRemoteCatalogDialogOnceAsync(
                    CreateRemoteCatalogDialogContext(
                        shellDependencies,
                        input.ResolveRuntimeFeature,
                        wmiServiceRecovery,
                        appDependencies.AppLogger),
                    request,
                    ct)
        };
    }

    private static MainRemoteCatalogDialogContext CreateRemoteCatalogDialogContext(
        MainShellResolvedDependencies shellDependencies,
        Func<MainRuntimeFeatureFacade> resolveRuntimeFeature,
        IWindowsWmiServiceRecovery wmiServiceRecovery,
        IAppLogger appLogger)
    {
        return new MainRemoteCatalogDialogContext
        {
            DialogGate = shellDependencies.RemoteCatalogDialogGate,
            FallbackErrorCode = MainViewModelStatusCodes.RuntimeDataFailed,
            ShowDialogAsync = shellDependencies.DialogPresenter.ShowSafelyAsync,
            HandleDialogResultAsync = (request, result, ct) =>
                HandleRemoteCatalogDialogResultAsync(
                    request,
                    result,
                    resolveRuntimeFeature,
                    wmiServiceRecovery,
                    shellDependencies.RemoteCatalogDialogGate,
                    appLogger,
                    ct)
        };
    }

    private static Task HandleRemoteCatalogDialogResultAsync(
        AppDialogRequest request,
        AppDialogResult result,
        Func<MainRuntimeFeatureFacade> resolveRuntimeFeature,
        IWindowsWmiServiceRecovery wmiServiceRecovery,
        OnceDialogGate remoteCatalogDialogGate,
        IAppLogger appLogger,
        CancellationToken cancellationToken)
    {
        MainRuntimeFeatureFacade? runtimeFeature = null;
        MainRuntimeFeatureFacade ResolveRuntimeFeatureOnce()
        {
            return runtimeFeature ??= resolveRuntimeFeature();
        }

        return HandleGpuDetectionRetryDialogResultAsync(
            request,
            result,
            wmiServiceRecovery.TryStartIfStoppedAsync,
            ct => ResolveRuntimeFeatureOnce().RefreshRuntimeContextAsync(ct),
            (mode, ct) => ResolveRuntimeFeatureOnce().RefreshRuntimeDataCatalogAsync(mode, ct),
            remoteCatalogDialogGate,
            appLogger,
            cancellationToken);
    }

    internal static Task HandleGpuDetectionRetryDialogResultAsync(
        AppDialogRequest request,
        AppDialogResult result,
        Func<CancellationToken, Task> tryStartWmiServiceAsync,
        Func<CancellationToken, Task> refreshRuntimeContextAsync,
        Func<RuntimeCatalogRefreshMode, CancellationToken, Task> refreshRuntimeCatalogAsync,
        OnceDialogGate remoteCatalogDialogGate,
        IAppLogger appLogger,
        CancellationToken cancellationToken,
        bool runInBackground = true,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tryStartWmiServiceAsync);
        ArgumentNullException.ThrowIfNull(refreshRuntimeContextAsync);
        ArgumentNullException.ThrowIfNull(refreshRuntimeCatalogAsync);
        ArgumentNullException.ThrowIfNull(remoteCatalogDialogGate);
        ArgumentNullException.ThrowIfNull(appLogger);

        if (result != AppDialogResult.Retry || !IsGpuDetectionFailedDialog(request))
        {
            return Task.CompletedTask;
        }

        remoteCatalogDialogGate.Reset();
        var retryTask = RunGpuDetectionRetryAsync(
            tryStartWmiServiceAsync,
            refreshRuntimeContextAsync,
            refreshRuntimeCatalogAsync,
            appLogger,
            cancellationToken,
            retryDelay ?? TimeSpan.FromMilliseconds(50));
        if (!runInBackground)
        {
            return retryTask;
        }

        _ = retryTask;
        return Task.CompletedTask;
    }

    private static async Task RunGpuDetectionRetryAsync(
        Func<CancellationToken, Task> tryStartWmiServiceAsync,
        Func<CancellationToken, Task> refreshRuntimeContextAsync,
        Func<RuntimeCatalogRefreshMode, CancellationToken, Task> refreshRuntimeCatalogAsync,
        IAppLogger appLogger,
        CancellationToken cancellationToken,
        TimeSpan retryDelay)
    {
        try
        {
            if (retryDelay > TimeSpan.Zero)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }

            appLogger.Info(MainViewModelLogCategories.Runtime, "gpu detection retry requested");
            await tryStartWmiServiceAsync(cancellationToken);

            await refreshRuntimeContextAsync(cancellationToken);
            await refreshRuntimeCatalogAsync(
                RuntimeCatalogRefreshMode.GpuDetectionRetry,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            appLogger.Warning(MainViewModelLogCategories.Runtime, "gpu detection retry canceled");
        }
        catch (Exception exception)
        {
            appLogger.Error(MainViewModelLogCategories.Runtime, "gpu detection retry failed", exception);
        }
    }

    private static bool IsGpuDetectionFailedDialog(AppDialogRequest request)
    {
        return string.Equals(
            (request.ErrorCode ?? "").Trim(),
            "gpu_detection_failed",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAppVersion(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "0.0.0" : normalized;
    }
}

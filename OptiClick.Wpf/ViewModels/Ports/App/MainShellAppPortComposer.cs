using OptiClick.Core.Runtime;
using OptiClick.Wpf.Shell.Dialogs;
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
            ReadAppVersion = () => NormalizeAppVersion(appDependencies.AppVersionProvider.GetCurrentVersion()),
            IsKoreanUi = () => access.SelectedLanguage == AppLanguage.Korean,
            SetSettingsStatusText = access.SetSettingsStatusText,
            SetScanStatusText = access.SetScanStatusText,
            ApplyStateUpdate = access.ApplyStateUpdate,
            ApplyDeferredStateUpdate = access.ApplyDeferredStateUpdate,
            ShouldBlockStartupForUnsupportedOperatingSystem =
                () => input.ResolveRuntimeFeature().IsUnsupportedWindows10OperatingSystem(),
            ShowRemoteCatalogDialogOnceAsync = (request, ct) =>
                startupDependencies.MainStartupDialogsController.ShowRemoteCatalogDialogOnceAsync(
                    CreateRemoteCatalogDialogContext(shellDependencies),
                    request,
                    ct)
        };
    }

    private static MainRemoteCatalogDialogContext CreateRemoteCatalogDialogContext(
        MainShellResolvedDependencies shellDependencies)
    {
        return new MainRemoteCatalogDialogContext
        {
            DialogGate = shellDependencies.RemoteCatalogDialogGate,
            FallbackErrorCode = MainViewModelStatusCodes.RuntimeDataFailed,
            ShowDialogAsync = shellDependencies.DialogPresenter.ShowSafelyAsync
        };
    }

    private static string NormalizeAppVersion(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "0.0.0" : normalized;
    }
}

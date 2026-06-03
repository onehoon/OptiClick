using OptiClick.Core.Abstractions;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Composition;

public sealed record AppSharedServices
{
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required IAppStringsProvider StringsProvider { get; init; }
    public required IWritableAppLanguageProvider LanguageProvider { get; init; }
    public required IExternalUrlLauncher ExternalUrlLauncher { get; init; }
    public required IAppUserSettingsStore UserSettingsStore { get; init; }
    public required IFirstRunStateStore FirstRunStateStore { get; init; }
    public required DialogHostViewModel DialogHost { get; init; }
    public required InstallManagementDialogHostViewModel InstallManagementDialogHost { get; init; }
    public required IDialogService DialogService { get; init; }
    public required IInstallManagementDialogService InstallManagementDialogService { get; init; }
    public required IRemoteEndpointProvider RemoteEndpointProvider { get; init; }
    public required IShellMockDataProvider MockDataProvider { get; init; }
}

public sealed class AppServicesComposition
{
    private readonly AppCompositionRoot _root;

    public AppServicesComposition(AppCompositionRoot root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public AppSharedServices CreateAppSharedServices()
    {
        var localDataPathProvider = _root.CreateAppLocalDataPathProvider();
        var remoteOptionsLoader = new RemoteDataOptionsLoader();
        var remoteEndpointProvider = new ConfigurationRemoteEndpointProvider(remoteOptionsLoader.Load());
        var appLogger = new FileAppLogger(localDataPathProvider.LogDirectory);
        var appVersionProvider = _root.CreateAppVersionProvider();
        var languageProvider = new SystemAppLanguageProvider(appLogger);
        var stringsProvider = new AppStringsProvider();
        var dialogHost = new DialogHostViewModel();
        var installManagementDialogHost = new InstallManagementDialogHostViewModel();
        var dialogService = new OverlayDialogService(dialogHost);
        var installManagementDialogService = new OverlayInstallManagementDialogService(installManagementDialogHost);
        var userSettingsStore = _root.CreateAppUserSettingsStore(localDataPathProvider, appLogger);
        var firstRunStateStore = _root.CreateFirstRunStateStore(localDataPathProvider, appLogger);
        var externalUrlLauncher = _root.CreateExternalUrlLauncher(appLogger);

        return new AppSharedServices
        {
            AppVersionProvider = appVersionProvider,
            LocalDataPathProvider = localDataPathProvider,
            AppLogger = appLogger,
            StringsProvider = stringsProvider,
            LanguageProvider = languageProvider,
            ExternalUrlLauncher = externalUrlLauncher,
            UserSettingsStore = userSettingsStore,
            FirstRunStateStore = firstRunStateStore,
            DialogHost = dialogHost,
            DialogService = dialogService,
            InstallManagementDialogHost = installManagementDialogHost,
            InstallManagementDialogService = installManagementDialogService,
            RemoteEndpointProvider = remoteEndpointProvider,
            MockDataProvider = new ShellMockDataProvider()
        };
    }
}

using OptiClick.Core.Abstractions;
using System.Net.Http;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Configuration;
using OptiClick.Wpf.Diagnostics;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Shell.Runtime.DeviceIdentity;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Install.UiState;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Services;
using OptiClick.Infrastructure.Storage;
using OptiClick.Infrastructure.Windows;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Scan;
using OptiClick.Wpf.ViewModels;
using OptiClickShell;

namespace OptiClick.Wpf.Composition;

public sealed partial class AppCompositionRoot
{
    public IAppLocalDataPathProvider CreateAppLocalDataPathProvider()
    {
        return new AppLocalDataPathProvider();
    }

    public IAppLogger CreateAppLogger()
    {
        var localDataPathProvider = CreateAppLocalDataPathProvider();
        return new FileAppLogger(localDataPathProvider.LogDirectory);
    }

    public IAppVersionProvider CreateAppVersionProvider()
    {
        return new AssemblyAppVersionProvider();
    }

    public IAppUpdateVersionComparer CreateAppUpdateVersionComparer()
    {
        return new AppUpdateVersionComparer();
    }

    public IAppUpdateService CreateAppUpdateService(IAppUpdateVersionComparer? versionComparer = null)
    {
        return new AppUpdateService(versionComparer ?? CreateAppUpdateVersionComparer());
    }

    public IAppUpdateExecutionService CreateAppUpdateExecutionService(
        IAppLocalDataPathProvider? localDataPathProvider = null,
        IAppLogger? logger = null,
        HttpClient? httpClient = null)
    {
        return new AppUpdateExecutionService(
            localDataPathProvider ?? CreateAppLocalDataPathProvider(),
            new ArchiveDownloader(httpClient ?? new HttpClient()),
            CreateArchiveExtractor(),
            logger);
    }

    public IFolderPickerService CreateFolderPickerService()
    {
        return new WindowsFolderPickerService();
    }

    public IScanFolderDiscoveryService CreateScanFolderDiscoveryService()
    {
        return new WindowsScanFolderDiscoveryService();
    }

    public IScanFileSystemProbe CreateScanFileSystemProbe()
    {
        return new ScanFileSystemProbe();
    }

    public IScanFolderManifestStore CreateScanFolderManifestStore(
        IAppLocalDataPathProvider? localDataPathProvider = null,
        IAppLogger? appLogger = null)
    {
        return new ScanFolderManifestStore(localDataPathProvider ?? CreateAppLocalDataPathProvider(), appLogger);
    }

    public IAppUserSettingsStore CreateAppUserSettingsStore(
        IAppLocalDataPathProvider? localDataPathProvider = null,
        IAppLogger? appLogger = null)
    {
        return new AppUserSettingsStore(localDataPathProvider ?? CreateAppLocalDataPathProvider(), appLogger);
    }

    public IFirstRunStateStore CreateFirstRunStateStore(
        IAppLocalDataPathProvider? localDataPathProvider = null,
        IAppLogger? appLogger = null)
    {
        return new FirstRunStateStore(localDataPathProvider ?? CreateAppLocalDataPathProvider(), appLogger);
    }

    public IContactIssueLinkBuilder CreateContactIssueLinkBuilder()
    {
        return new ContactIssueLinkBuilder();
    }

    public IExternalUrlLauncher CreateExternalUrlLauncher(IAppLogger? appLogger = null)
    {
        return new ExternalUrlLauncher(appLogger);
    }

    public IProcessElevationService CreateProcessElevationService(IAppLogger? appLogger = null)
    {
        return new ProcessElevationService(appLogger);
    }

    public bool IsCurrentProcessElevated(
        IProcessElevationService? processElevationService = null)
    {
        var service = processElevationService ?? CreateProcessElevationService();
        return service.IsCurrentProcessElevated();
    }

    public bool ShouldRelaunchElevated(
        string[] args,
        IProcessElevationService? processElevationService = null)
    {
        var safeArgs = args ?? [];

        if (safeArgs.Any(arg => string.Equals(arg, ProcessElevationService.ElevatedRelaunchArgument, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var service = processElevationService ?? CreateProcessElevationService();
        return !service.IsCurrentProcessElevated();
    }

    public bool TryRelaunchAsAdministrator(
        string[] args,
        IProcessElevationService? processElevationService = null)
    {
        var service = processElevationService ?? CreateProcessElevationService();
        return service.TryRelaunchAsAdministrator(args ?? []);
    }
}



using OptiClick.Core.Abstractions;
using System.Net.Http;
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
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.ViewModels;
using OptiClickShell;

namespace OptiClick.Wpf.Composition;

public sealed partial class AppCompositionRoot
{
    public AppSharedServices CreateAppSharedServices()
    {
        var composition = new AppServicesComposition(this);
        return composition.CreateAppSharedServices();
    }

    public RuntimeCompositionServices CreateRuntimeServices(AppSharedServices app)
    {
        var composition = new RuntimeComposition(this);
        return composition.CreateRuntimeServices(app);
    }

    public ScanCompositionServices CreateScanServices(AppSharedServices app, RuntimeCompositionServices runtime)
    {
        var composition = new ScanComposition(this);
        return composition.CreateScanServices(app, runtime);
    }

    public InstallCompositionServices CreateInstallServices(AppSharedServices app)
    {
        var composition = new InstallComposition(this);
        return composition.CreateInstallServices(app);
    }

    public UpdateCompositionServices CreateUpdateServices(AppSharedServices app)
    {
        var composition = new UpdateComposition(this);
        return composition.CreateUpdateServices(app);
    }

    public SupportCompositionServices CreateSupportServices(AppSharedServices app)
    {
        var composition = new SupportComposition(this);
        return composition.CreateSupportServices(app);
    }

    public MainViewModel CreateMainViewModel(
        AppSharedServices app,
        RuntimeCompositionServices runtime,
        ScanCompositionServices scan,
        InstallCompositionServices install,
        UpdateCompositionServices update,
        SupportCompositionServices support)
    {
        var composition = new MainWindowComposition();
        return composition.CreateMainViewModel(
            app,
            runtime,
            scan,
            install,
            update,
            support,
            seedMockGameCards: false,
            seedMockScanFolders: false);
    }

    public MainWindow CreateMainWindow()
    {
        var app = CreateAppSharedServices();
        var runtime = CreateRuntimeServices(app);
        var scan = CreateScanServices(app, runtime);
        var install = CreateInstallServices(app);
        var update = CreateUpdateServices(app);
        var support = CreateSupportServices(app);
        var viewModel = CreateMainViewModel(app, runtime, scan, install, update, support);
        return new MainWindow(viewModel, app.AppLogger);
    }
}

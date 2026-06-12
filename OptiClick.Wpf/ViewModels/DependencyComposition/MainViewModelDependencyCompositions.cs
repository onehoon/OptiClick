using OptiClick.Core.Abstractions;
using OptiClick.Core.OptiScaler;
using OptiClick.Core.Scan;
using OptiClick.Wpf.Composition.Modules;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Actions;
using OptiClick.Wpf.Shell.Dialogs;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Games.GpuBundle;
using OptiClick.Wpf.Shell.Gpu;
using OptiClick.Wpf.Shell.Localization;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Runtime;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Shell.Selection;
using OptiClick.Wpf.Shell.Settings;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Support;
using OptiClick.Wpf.Shell.Update;
using OptiClick.Wpf.Shell.Wiki;
using OptiClick.Wpf.ViewModels.Sections;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.Shell;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal sealed record RuntimeDependencyComposition
{
    public required IOperatingSystemSupportPolicy OperatingSystemSupportPolicy { get; init; }
    public required IShellGameCardViewModelFactory? ShellGameCardViewModelFactory { get; init; }
    public required RuntimeContextFlowController RuntimeContextFlowController { get; init; }
    public required DeviceIdentityRulesFlowController DeviceIdentityRulesFlowController { get; init; }
    public required RuntimeCatalogFlowController RuntimeCatalogFlowController { get; init; }
    public required RuntimeEndpointStatusPresenter RuntimeEndpointStatusPresenter { get; init; }
    public required MainRuntimeCatalogUiFlowController RuntimeCatalogUiFlowController { get; init; }
    public required GpuSelectionCoordinator GpuSelectionCoordinator { get; init; }
    public required RuntimeCatalogCoordinator RuntimeCatalogCoordinator { get; init; }
    public required IRemoteGpuBundleManifestClient GpuBundleManifestClient { get; init; }
    public required IGpuBundleManifestRuleResolver GpuBundleManifestRuleResolver { get; init; }
    public required IDeviceIdentityResolver DeviceIdentityResolver { get; init; }
}

internal sealed record ScanDependencyComposition
{
    public required IFolderPickerService? FolderPickerService { get; init; }
    public required IScanFolderDiscoveryService? ScanFolderDiscoveryService { get; init; }
    public required IScanFileSystemProbe ScanFileSystemProbe { get; init; }
    public required ScanFlowController ScanFlowController { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required ScanResultCoordinatorFactory ScanResultCoordinatorFactory { get; init; }
    public required ScanOrchestratorFactory ScanOrchestratorFactory { get; init; }
    public required ScanVisibleGameResolver ScanVisibleGameResolver { get; init; }
}

internal sealed record InstallDependencyComposition
{
    public required GameSelectionFlowController GameSelectionFlowController { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required InstallPopupPresenter InstallPopupPresenter { get; init; }
    public required InstallFlowController InstallFlowController { get; init; }
    public required IOptiClickUninstallPlanBuilder OptiClickUninstallPlanBuilder { get; init; }
    public required IOptiClickUninstallExecutor OptiClickUninstallExecutor { get; init; }
}

internal sealed record AppDependencyComposition
{
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required AppUpdateFlowController AppUpdateFlowController { get; init; }
    public required AppUpdateCoordinator AppUpdateCoordinator { get; init; }
    public required ShellUiServices ShellUiServices { get; init; }
    public required ShellDialogServices ShellDialogServices { get; init; }
    public required ShellSelectionModuleCompositionServices ShellSelectionServices { get; init; }
    public required ShellSupportModuleCompositionServices ShellSupportServices { get; init; }
    public required ShellSectionServices ShellSectionServices { get; init; }
    public required RuntimeModuleCompositionServices RuntimeServices { get; init; }
    public required StartupModuleCompositionServices StartupServices { get; init; }
    public required InstallModuleCompositionServices InstallServices { get; init; }
    public required OptiScalerSettingsModuleCompositionServices OptiScalerSettingsServices { get; init; }
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppStringsProvider AppStringsProvider { get; init; }
    public required IFirstRunStateStore FirstRunStateStore { get; init; }
}

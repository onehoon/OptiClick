using OptiClick.Wpf.Install.Fallbacks;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Install.Uninstall;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.ViewModels;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.Composition;

internal static class MainViewModelInstallFallbackFactory
{
    public static MainViewModelInstallFallbackServices Create(
        MainViewModelInstallDependencies installDependencies,
        IAppLogger appLogger)
    {
        ArgumentNullException.ThrowIfNull(installDependencies);
        ArgumentNullException.ThrowIfNull(appLogger);

        var resolvedUninstallFileSystem = new InstallFileSystem();
        var resolvedUninstallSignatures = new FileSignatureDetectors(resolvedUninstallFileSystem);
        var resolvedUninstallVersionInfoReader = new WindowsFileVersionInfoReader();
        var optiClickUninstallPlanBuilder = installDependencies.OptiClickUninstallPlanBuilder
                                            ?? new OptiClickUninstallPlanBuilder(
                                                resolvedUninstallFileSystem,
                                                resolvedUninstallSignatures,
                                                resolvedUninstallVersionInfoReader,
                                                appLogger);
        var optiClickUninstallExecutor = installDependencies.OptiClickUninstallExecutor
                                         ?? new OptiClickUninstallExecutor(
                                             resolvedUninstallFileSystem,
                                             resolvedUninstallSignatures,
                                             resolvedUninstallVersionInfoReader,
                                             appLogger);

        return new MainViewModelInstallFallbackServices
        {
            InstallPlanBuilder = new UnavailableInstallPlanBuilder(),
            InstallStartGateResolver = new UnavailableInstallStartGateResolver(),
            ComponentInstallCoordinator = new UnavailableComponentInstallCoordinator(),
            ComponentInstallParityReviewBuilder = new UnavailableComponentInstallParityReviewBuilder(),
            OptiClickUninstallPlanBuilder = optiClickUninstallPlanBuilder,
            OptiClickUninstallExecutor = optiClickUninstallExecutor
        };
    }

    public static MainViewModelInstallFallbackServices CreateExplicit(
        MainViewModelInstallDependencies installDependencies)
    {
        ArgumentNullException.ThrowIfNull(installDependencies);

        return new MainViewModelInstallFallbackServices
        {
            InstallPlanBuilder = Require(
                installDependencies.InstallPlanBuilder,
                nameof(MainViewModelInstallDependencies.InstallPlanBuilder)),
            InstallStartGateResolver = Require(
                installDependencies.InstallStartGateResolver,
                nameof(MainViewModelInstallDependencies.InstallStartGateResolver)),
            ComponentInstallCoordinator = Require(
                installDependencies.ComponentInstallCoordinator,
                nameof(MainViewModelInstallDependencies.ComponentInstallCoordinator)),
            ComponentInstallParityReviewBuilder = Require(
                installDependencies.ComponentInstallParityReviewBuilder,
                nameof(MainViewModelInstallDependencies.ComponentInstallParityReviewBuilder)),
            OptiClickUninstallPlanBuilder = Require(
                installDependencies.OptiClickUninstallPlanBuilder,
                nameof(MainViewModelInstallDependencies.OptiClickUninstallPlanBuilder)),
            OptiClickUninstallExecutor = Require(
                installDependencies.OptiClickUninstallExecutor,
                nameof(MainViewModelInstallDependencies.OptiClickUninstallExecutor))
        };
    }

    private static T Require<T>(T? dependency, string dependencyName)
        where T : class
    {
        return dependency
               ?? throw new InvalidOperationException(
                   $"MainViewModel install dependency '{dependencyName}' must be provided when fallback resolution is disabled.");
    }
}

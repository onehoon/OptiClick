using OptiClick.Wpf.Install.Gates;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels.Sections.Scan;
using OptiClick.Wpf.ViewModels.DependencyComposition;

namespace OptiClick.Wpf.ViewModels;

internal static partial class MainViewModelFallbackDependencyResolver
{
    private static ScanDependencyComposition ResolveScanDependencies(
        MainViewModelScanDependencies scanDependencies,
        MainViewModelRuntimeDependencies runtimeDependencies)
    {
        return MainViewModelFallbackScanDependencyComposer.Compose(scanDependencies, runtimeDependencies);
    }
}

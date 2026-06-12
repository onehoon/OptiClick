using OptiClick.Core.Abstractions;
using OptiClick.Core.OptiScaler;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.ViewModels.DependencyComposition;

internal sealed record MainViewModelAppFallbackServices
{
    public required IAppLocalDataPathProvider LocalDataPathProvider { get; init; }
    public required IAppUserSettingsStore UserSettingsStore { get; init; }
    public required IFirstRunStateStore FirstRunStateStore { get; init; }
    public required IAppVersionProvider AppVersionProvider { get; init; }
    public required IAppUpdateService AppUpdateService { get; init; }
    public required IAppUpdateExecutionService AppUpdateExecutionService { get; init; }
    public required IExternalUrlLauncher ExternalUrlLauncher { get; init; }
    public required IOptiScalerSettingsApplicationService OptiScalerSettingsApplicationService { get; init; }
}

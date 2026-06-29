using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Flow;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallArchiveReadinessContextFactory
{
    private readonly MainInstallArchiveReadinessContextFactoryInput _input;

    public MainInstallArchiveReadinessContextFactory(MainInstallArchiveReadinessContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainInstallArchiveReadinessContext Create(bool refreshVisibleGamesAfterArchiveReadiness = true)
    {
        return new MainInstallArchiveReadinessContext
        {
            State = new MainInstallArchiveReadinessState
            {
                ModuleDownloadLinks = _input.ReadModuleDownloadLinks(),
                LatestOptiScalerVariantCatalog = _input.ReadLatestOptiScalerVariantCatalog(),
                PreferredOptiScalerVariant = _input.ReadPreferredOptiScalerVariant(),
                GpuBundleKey = _input.ReadGpuBundleKey(),
                LatestArchiveReadiness = _input.ReadLatestArchiveReadiness(),
                RefreshVisibleGamesAfterArchiveReadiness = refreshVisibleGamesAfterArchiveReadiness
            },
            Services = new MainInstallArchiveReadinessServices
            {
                ArchiveReadinessRefreshCoordinator = _input.ArchiveReadinessRefreshCoordinator,
                ArchiveReadinessFlowController = _input.ArchiveReadinessFlowController,
                DispatchFlowLogs = _input.DispatchFlowLogs
            },
            Callbacks = new MainInstallArchiveReadinessCallbacks
            {
                SetArchiveReadiness = _input.SetArchiveReadiness,
                ApplyOptiScalerVariantSyncToRuntimeState =
                    _input.ApplyOptiScalerVariantSyncToRuntimeState,
                ApplyOptiScalerVariantOptions = _input.ApplyOptiScalerVariantOptions,
                RefreshVisibleGamesAfterArchiveReadiness =
                    _input.RefreshVisibleGamesAfterArchiveReadiness,
                PersistEffectiveVariantPreference = _input.PersistEffectiveVariantPreference,
                SaveUserSettings = _input.SaveUserSettings
            }
        };
    }
}

internal sealed record MainInstallArchiveReadinessContextFactoryInput
{
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Func<OptiScalerVariantCatalog> ReadLatestOptiScalerVariantCatalog { get; init; }
    public required Func<string> ReadPreferredOptiScalerVariant { get; init; }
    public required Func<string> ReadGpuBundleKey { get; init; }
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required ArchiveReadinessRefreshCoordinator ArchiveReadinessRefreshCoordinator { get; init; }
    public required ArchiveReadinessFlowController ArchiveReadinessFlowController { get; init; }
    public required Action<IReadOnlyList<IFlowLogEntry>, string> DispatchFlowLogs { get; init; }
    public required Action<ArchiveReadinessSnapshot> SetArchiveReadiness { get; init; }
    public required Action<OptiScalerVariantSyncResult?> ApplyOptiScalerVariantSyncToRuntimeState { get; init; }
    public required Action ApplyOptiScalerVariantOptions { get; init; }
    public required Action RefreshVisibleGamesAfterArchiveReadiness { get; init; }
    public required Action<string> PersistEffectiveVariantPreference { get; init; }
    public required Action SaveUserSettings { get; init; }
}

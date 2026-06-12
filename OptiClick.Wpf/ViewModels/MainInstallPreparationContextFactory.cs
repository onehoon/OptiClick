using OptiClick.Core.OptiScaler;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallPreparationContextFactory
{
    private readonly MainInstallPreparationContextFactoryInput _input;

    public MainInstallPreparationContextFactory(MainInstallPreparationContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainInstallPreparationContext Create()
    {
        return new MainInstallPreparationContext
        {
            State = new MainInstallPreparationState
            {
                IsInstallExecutionInProgress = _input.IsInstallExecutionInProgress,
                IsAppUpdateInProgress = _input.IsAppUpdateInProgress,
                ResolveSelectedGame = _input.ResolveSelectedGame,
                ResolveSelectedIndex = _input.ResolveSelectedIndex,
                LatestRuntimeContext = _input.ReadLatestRuntimeContext(),
                ReadLatestArchiveReadiness = _input.ReadLatestArchiveReadiness,
                ReadSelectionState = _input.ReadSelectionState,
                ModuleDownloadLinks = _input.ReadModuleDownloadLinks(),
                IsOperatingSystemSupported = _input.IsOperatingSystemSupported,
                Text = _input.ReadInstallFlowText(),
                LatestRemoteCatalogErrorCode = _input.ReadLatestRemoteCatalogErrorCode()
            },
            Services = new MainInstallPreparationServices
            {
                RefreshArchiveReadinessAsync = _input.RefreshArchiveReadinessAsync,
                RefreshSelectionForInstallAsync = _input.RefreshSelectionForInstallAsync,
                CreateOptiScalerIniApplyContext = _input.CreateOptiScalerIniApplyContext,
                BuildInstallRequest = _input.FlowRequestFactory.BuildInstallRequest
            }
        };
    }
}

internal sealed record MainInstallPreparationContextFactoryInput
{
    public required Func<bool> IsInstallExecutionInProgress { get; init; }
    public required Func<bool> IsAppUpdateInProgress { get; init; }
    public required Func<GameCardViewModel?> ResolveSelectedGame { get; init; }
    public required Func<GameCardViewModel, int> ResolveSelectedIndex { get; init; }
    public required Func<RuntimeContext> ReadLatestRuntimeContext { get; init; }
    public required Func<ArchiveReadinessSnapshot> ReadLatestArchiveReadiness { get; init; }
    public required Func<ShellInstallSelectionState> ReadSelectionState { get; init; }
    public required Func<ModuleDownloadLinkContext> ReadModuleDownloadLinks { get; init; }
    public required Func<bool> IsOperatingSystemSupported { get; init; }
    public required Func<InstallFlowText> ReadInstallFlowText { get; init; }
    public required Func<string> ReadLatestRemoteCatalogErrorCode { get; init; }
    public required Func<CancellationToken, Task<ArchiveReadinessFlowResult>> RefreshArchiveReadinessAsync { get; init; }
    public required Func<GameCardViewModel, CancellationToken, bool, bool, Task> RefreshSelectionForInstallAsync { get; init; }
    public required Func<OptiScalerIniApplyContext> CreateOptiScalerIniApplyContext { get; init; }
    public required MainViewModelFlowRequestFactory FlowRequestFactory { get; init; }
}

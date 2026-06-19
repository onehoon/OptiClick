using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Shell.Selection;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainInstallExecutionBridgeContextFactory
{
    private readonly MainInstallExecutionBridgeContextFactoryInput _input;

    public MainInstallExecutionBridgeContextFactory(MainInstallExecutionBridgeContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainInstallExecutionBridgeContext Create(MainInstallExecutionPreparation preparation)
    {
        return new MainInstallExecutionBridgeContext
        {
            Preparation = preparation,
            Services = new MainInstallExecutionBridgeServices
            {
                InstallExecutionCoordinator = _input.InstallExecutionCoordinator,
                MainInstallCompletionController = _input.MainInstallCompletionController
            },
            Callbacks = new MainInstallExecutionBridgeCallbacks
            {
                ApplyInstallBusyState = _input.ApplyInstallBusyState,
                CreateInstallCompletionContext = _input.CreateInstallCompletionContext,
                ReadInstallExecutionText = _input.ReadInstallExecutionText
            }
        };
    }
}

internal sealed record MainInstallExecutionBridgeContextFactoryInput
{
    public required InstallExecutionCoordinator InstallExecutionCoordinator { get; init; }
    public required MainInstallCompletionController MainInstallCompletionController { get; init; }
    public required Action<bool, string, ShellInstallSelectionState?> ApplyInstallBusyState { get; init; }
    public required Func<ShellInstallSelectionState?, MainInstallCompletionContext> CreateInstallCompletionContext { get; init; }
    public required Func<InstallExecutionCoordinatorText> ReadInstallExecutionText { get; init; }
}

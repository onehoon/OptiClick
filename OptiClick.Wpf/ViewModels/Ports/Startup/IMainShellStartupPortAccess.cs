using OptiClick.Wpf.Shell.Startup;

namespace OptiClick.Wpf.ViewModels.Ports.Startup;

internal interface IMainShellStartupPortAccess
{
    void UpdateStartupPreparationState(Func<StartupPreparationState, StartupPreparationState> update);
    string ClearLastErrorCode(string lastErrorCode, string errorCode);
    Task RunStartupAutoScanAsync(CancellationToken cancellationToken);
}

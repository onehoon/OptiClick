using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.ShellCommand;
using OptiClick.Wpf.ViewModels.Ports.ShellInteractionContext.StartupAnnouncement;

namespace OptiClick.Wpf.ViewModels;

internal sealed class MainShellInteractionContextFactory
{
    private readonly MainShellInteractionContextFactoryInput _input;

    public MainShellInteractionContextFactory(MainShellInteractionContextFactoryInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public MainShellCommandInteractionContext CreateShellCommandContext()
    {
        var input = _input.ShellCommand;

        return new MainShellCommandInteractionContext
        {
            CurrentAppVersion = input.ReadCurrentAppVersion(),
            RuntimeContext = input.ReadLatestRuntimeContext(),
            SelectedLanguage = input.ReadSelectedLanguage(),
            Strings = input.ReadStrings(),
            LogDirectory = input.ReadLogDirectory(),
            ResultApplier = input.ResultApplier,
            ApplyStateUpdate = input.ApplyStateUpdate,
            ApplyDeferredStateUpdate = input.ApplyDeferredStateUpdate,
            ShowDeferredDialog = input.ShowDeferredDialog,
            ApplyAppLog = input.ApplyAppLog,
            ShowDialogAsync = input.ShowDialogAsync
        };
    }

    public MainStartupAnnouncementInteractionContext CreateStartupAnnouncementContext()
    {
        var input = _input.StartupAnnouncement;

        return new MainStartupAnnouncementInteractionContext
        {
            RuntimeData = input.ReadLatestRuntimeData(),
            Language = input.ReadSelectedLanguage(),
            SelectedGpuVendor = input.ReadSelectedGpuVendor(),
            DispatchFlowLogs = input.DispatchFlowLogs,
            ShowDialogAsync = input.ShowDialogAsync,
            OpenExternalUrl = input.OpenExternalUrl,
            LogWarning = input.LogWarning
        };
    }

    public MainUserSettingsApplyContext CreateUserSettingsApplyContext()
    {
        var input = _input.UserSettings;

        return new MainUserSettingsApplyContext
        {
            NormalizeLanguagePreference = input.NormalizeLanguagePreference,
            NormalizeOptiScalerVariantPreference = input.NormalizeOptiScalerVariantPreference,
            ResolvePreferredLanguage = input.ResolvePreferredLanguage,
            ResolveLanguageOptionFromState = input.ResolveLanguageOptionFromState,
            SetLanguagePreference = input.SetLanguagePreference,
            SetOptiScalerVariantPreference = input.SetOptiScalerVariantPreference,
            ReadSelectedLanguage = input.ReadSelectedLanguage,
            ApplyChangedLanguage = input.ApplyChangedLanguage,
            ApplyLoadedSettings = input.ApplyLoadedSettings,
            ApplySavedOptiScalerSettings = input.ApplySavedOptiScalerSettings,
            LoadCommonIniSettings = input.LoadCommonIniSettings
        };
    }

    public MainAppUpdateInteractionContext CreateAppUpdateInteractionContext()
    {
        var input = _input.AppUpdate;

        return new MainAppUpdateInteractionContext
        {
            AppUpdateCoordinator = input.AppUpdateCoordinator,
            AppUpdateFlowController = input.AppUpdateFlowController,
            BusyStateApplier = input.BusyStateApplier,
            ResultApplier = input.ResultApplier,
            ReadStrings = input.ReadStrings,
            ReadLatestRuntimeData = input.ReadLatestRuntimeData,
            ReadCurrentAppVersion = input.ReadCurrentAppVersion,
            IsAppUpdateInProgress = input.IsAppUpdateInProgress,
            IsInstallExecutionInProgress = input.IsInstallExecutionInProgress,
            ReadSelectionState = input.ReadSelectionState,
            SetSettingsStatusText = input.SetSettingsStatusText,
            DispatchFlowLogs = input.DispatchFlowLogs,
            ShowDialogAsync = input.ShowDialogAsync,
            ApplyBusyStateUpdate = input.ApplyBusyStateUpdate,
            ApplyStateUpdate = input.ApplyStateUpdate,
            LogError = input.LogError,
            ShutdownApplication = input.ShutdownApplication
        };
    }

}

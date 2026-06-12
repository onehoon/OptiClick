using OptiClick.Core.Install;
using OptiClick.Infrastructure.Install.Config;
using OptiClick.Wpf.Install.Config;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Presentation;
using OptiClick.Wpf.Install.UiState;

namespace OptiClick.Wpf.Composition.Modules;

internal sealed record ConfigApplyCompositionServices
{
    public required ConfigApplyFlowController ConfigApplyFlowController { get; init; }
    public required IInstallResultApplier InstallResultApplier { get; init; }
}

internal sealed record ConfigApplyCompositionRequest
{
    public IConfigProfileApplier? ConfigProfileApplier { get; init; }
    public IniProfileEditor? IniProfileEditor { get; init; }
    public ConfigApplyFlowController? ConfigApplyFlowController { get; init; }
    public IInstallResultApplier? InstallResultApplier { get; init; }
    public IInstallResultPresentationResolver? InstallResultPresentationResolver { get; init; }
    public required InstallCompletionMessageBuilder InstallCompletionMessageBuilder { get; init; }
}

internal static class ConfigApplyCompositionFactory
{
    public static ConfigApplyCompositionServices Create(ConfigApplyCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var configApplyApplicationService = CreateConfigApplyApplicationService(request);
        var configApplyFlowController = request.ConfigApplyFlowController
                                        ?? CreateConfigApplyFlowController(configApplyApplicationService);
        var installResultApplier = request.InstallResultApplier
                                   ?? new InstallResultApplier(
                                       configApplyApplicationService,
                                       new RtssProfileApplier(),
                                       request.InstallResultPresentationResolver,
                                       request.InstallCompletionMessageBuilder);

        return new ConfigApplyCompositionServices
        {
            ConfigApplyFlowController = configApplyFlowController,
            InstallResultApplier = installResultApplier
        };
    }

    private static ConfigApplyApplicationService CreateConfigApplyApplicationService(
        ConfigApplyCompositionRequest request)
    {
        var optiScalerIniBaseApplier = request.IniProfileEditor is null
            ? null
            : new OptiScalerIniBaseApplier(request.IniProfileEditor);
        return new ConfigApplyApplicationService(
            request.ConfigProfileApplier is null
                ? null
                : new ConfigApplyProfileStageRunner(request.ConfigProfileApplier),
            optiScalerIniBaseApplier is null
                ? null
                : new ConfigApplyOptiScalerIniStageRunner(optiScalerIniBaseApplier));
    }

    private static ConfigApplyFlowController CreateConfigApplyFlowController(
        ConfigApplyApplicationService applicationService)
    {
        return new ConfigApplyFlowController(
            applicationService,
            new ConfigApplyInstallLogAdapter());
    }
}

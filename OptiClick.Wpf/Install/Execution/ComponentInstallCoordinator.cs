using System.IO;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games;
namespace OptiClick.Wpf.Install.Execution;

public interface IComponentInstallCoordinator
{
    Task<ComponentInstallResult> ExecuteAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ComponentInstallCoordinator : IComponentInstallCoordinator
{
    private readonly IOptiScalerCoreInstaller _coreInstaller;
    private readonly IExtraBundleInstaller _extraBundleInstaller;
    private readonly IUltimateAsiLoaderInstaller _ualInstaller;
    private readonly ISpecialKInstaller _specialKInstaller;
    private readonly IReFrameworkInstaller _reFrameworkInstaller;
    private readonly IOptiPatcherInstaller _optiPatcherInstaller;
    private readonly IUnreal5Installer _unreal5Installer;
    private readonly IFsr4Installer _fsr4Installer;
    private readonly IAppLogger _logger;

    public ComponentInstallCoordinator(
        IOptiScalerCoreInstaller coreInstaller,
        IExtraBundleInstaller extraBundleInstaller,
        IUltimateAsiLoaderInstaller ualInstaller,
        ISpecialKInstaller specialKInstaller,
        IReFrameworkInstaller reFrameworkInstaller,
        IOptiPatcherInstaller optiPatcherInstaller,
        IUnreal5Installer unreal5Installer,
        IFsr4Installer fsr4Installer,
        IAppLogger? logger = null)
    {
        _coreInstaller = coreInstaller;
        _extraBundleInstaller = extraBundleInstaller;
        _ualInstaller = ualInstaller;
        _specialKInstaller = specialKInstaller;
        _reFrameworkInstaller = reFrameworkInstaller;
        _optiPatcherInstaller = optiPatcherInstaller;
        _unreal5Installer = unreal5Installer;
        _fsr4Installer = fsr4Installer;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<ComponentInstallResult> ExecuteAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var steps = new List<ComponentInstallStepResult>();
        var executionContext = PrepareExecutionContext(context);

        var core = _coreInstaller.Install(executionContext);
        steps.Add(core);
        if (core.Status == ComponentInstallStatus.Failed)
        {
            _logger.Error(
                "Install",
                $"execution failed component={ComponentInstallName.OptiScalerCore} code={core.ErrorCode} message={NormalizeStepMessage(core.Message)}");
            return Failed(steps, core);
        }
        executionContext = UpdateFinalDllNameFromCoreStep(executionContext, core);

        // ExtraBundle runs last so game-specific OptiScaler override payloads win over the base install.
        var inOrder = new (ComponentInstallName Name, Func<Task<ComponentInstallStepResult>> Run)[]
        {
            (ComponentInstallName.UltimateAsiLoader, () => _ualInstaller.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.SpecialK, () => _specialKInstaller.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.ReFramework, () => _reFrameworkInstaller.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.OptiPatcher, () => _optiPatcherInstaller.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.Unreal5, () => _unreal5Installer.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.Fsr4, () => _fsr4Installer.InstallAsync(executionContext, cancellationToken)),
            (ComponentInstallName.ExtraBundle, () => _extraBundleInstaller.InstallAsync(executionContext, cancellationToken))
        };

        foreach (var step in inOrder)
        {
            var result = await step.Run();
            steps.Add(result);
            if (result.Status == ComponentInstallStatus.Failed)
            {
                _logger.Error(
                    "Install",
                    $"execution failed component={result.Component} code={result.ErrorCode} message={NormalizeStepMessage(result.Message)}");
                return Failed(steps, result);
            }
        }

        return new ComponentInstallResult
        {
            IsSuccess = true,
            Steps = steps
        };
    }

    private static ComponentInstallResult Failed(IReadOnlyList<ComponentInstallStepResult> steps, ComponentInstallStepResult failedStep)
    {
        return new ComponentInstallResult
        {
            IsSuccess = false,
            Steps = steps,
            FailedStep = failedStep
        };
    }

    private ComponentInstallContext PrepareExecutionContext(ComponentInstallContext context)
    {
        var preferredProxyName = ResolvePreferredProxyName(context);
        var normalizedContext = context;
        if (!string.IsNullOrWhiteSpace(preferredProxyName)
            && !string.Equals(preferredProxyName, context.FinalDllName, StringComparison.OrdinalIgnoreCase))
        {
            normalizedContext = context with
            {
                FinalDllName = preferredProxyName
            };
        }

        if (normalizedContext.UseUltimateAsiLoader || normalizedContext.UalDetectedNames.Count > 0)
        {
            return normalizedContext;
        }

        _specialKInstaller.CleanupRootSpecialKBeforeProxyResolution(
            normalizedContext.TargetPath,
            ShellGameInstallMetadataResolver.GetSpecialK(normalizedContext.Game),
            preferredProxyName);
        return normalizedContext;
    }

    private static string ResolvePreferredProxyName(ComponentInstallContext context)
    {
        var preferred = ShellGameInstallMetadataResolver.GetOptiScalerDllName(context.Game);
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        return (context.FinalDllName ?? "").Trim();
    }

    private static ComponentInstallContext UpdateFinalDllNameFromCoreStep(
        ComponentInstallContext context,
        ComponentInstallStepResult coreStep)
    {
        if (coreStep.Status != ComponentInstallStatus.Success || coreStep.Operations.Count == 0)
        {
            return context;
        }

        var rename = coreStep.Operations.LastOrDefault(static operation =>
            string.Equals(operation.Kind, "rename", StringComparison.OrdinalIgnoreCase));
        if (rename is null || string.IsNullOrWhiteSpace(rename.Destination))
        {
            return context;
        }

        var resolvedFileName = Path.GetFileName(rename.Destination);
        if (string.IsNullOrWhiteSpace(resolvedFileName)
            || string.Equals(resolvedFileName, context.FinalDllName, StringComparison.OrdinalIgnoreCase))
        {
            return context;
        }

        return context with
        {
            FinalDllName = resolvedFileName
        };
    }

    private static string NormalizeStepMessage(string? message)
    {
        var normalized = (message ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "-" : normalized;
    }
}

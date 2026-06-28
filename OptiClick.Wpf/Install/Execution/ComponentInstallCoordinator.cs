using System.IO;
using OptiClick.Wpf.Logging;
using CoreComponentInstallExecutionOrderPolicy = OptiClick.Core.Install.Components.ComponentInstallExecutionOrderPolicy;

namespace OptiClick.Wpf.Install.Execution;

public interface IComponentInstallCoordinator
{
    Task<ComponentInstallResult> ExecuteAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ComponentInstallCoordinator : IComponentInstallCoordinator
{
    private readonly IOptiScalerCoreInstaller _coreInstaller;
    private readonly IExtraBundleInstaller _extraBundleInstaller;
    private readonly ISpecialKInstaller _specialKInstaller;
    private readonly IReFrameworkInstaller _reFrameworkInstaller;
    private readonly IUnreal5Installer _unreal5Installer;
    private readonly IAppLogger _logger;

    public ComponentInstallCoordinator(
        IOptiScalerCoreInstaller coreInstaller,
        IExtraBundleInstaller extraBundleInstaller,
        ISpecialKInstaller specialKInstaller,
        IReFrameworkInstaller reFrameworkInstaller,
        IUnreal5Installer unreal5Installer,
        IAppLogger? logger = null)
    {
        _coreInstaller = coreInstaller;
        _extraBundleInstaller = extraBundleInstaller;
        _specialKInstaller = specialKInstaller;
        _reFrameworkInstaller = reFrameworkInstaller;
        _unreal5Installer = unreal5Installer;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<ComponentInstallResult> ExecuteAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var steps = new List<ComponentInstallStepResult>();
        var executionOrder = ResolveExecutionOrder(context);
        var executionContext = PrepareExecutionContext(context, executionOrder);

        if (executionOrder.Contains(ComponentInstallName.OptiScalerCore))
        {
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
        }

        foreach (var component in executionOrder.Where(static component => component != ComponentInstallName.OptiScalerCore))
        {
            var result = await RunPostCoreComponentAsync(component, executionContext, cancellationToken);
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

    private Task<ComponentInstallStepResult> RunPostCoreComponentAsync(
        ComponentInstallName component,
        ComponentInstallContext executionContext,
        CancellationToken cancellationToken)
    {
        return component switch
        {
            ComponentInstallName.SpecialK => _specialKInstaller.InstallAsync(executionContext, cancellationToken),
            ComponentInstallName.ReFramework => _reFrameworkInstaller.InstallAsync(executionContext, cancellationToken),
            ComponentInstallName.Unreal5 => _unreal5Installer.InstallAsync(executionContext, cancellationToken),
            ComponentInstallName.ExtraBundle => _extraBundleInstaller.InstallAsync(executionContext, cancellationToken),
            _ => throw new InvalidOperationException(
                "Infrastructure component execution supports only post-core components. OptiScalerCore is handled separately via IOptiScalerCoreInstaller.")
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

    private static IReadOnlyList<ComponentInstallName> ResolveExecutionOrder(ComponentInstallContext context)
    {
        var fullOrder = CoreComponentInstallExecutionOrderPolicy.GetCoreThenMiddleThenExtraOrder();

        if (!context.HasPlannedComponentInstallers)
        {
            return fullOrder;
        }

        var planned = context.PlannedComponentInstallers.ToHashSet();
        return fullOrder.Where(planned.Contains).ToArray();
    }

    private ComponentInstallContext PrepareExecutionContext(
        ComponentInstallContext context,
        IReadOnlyList<ComponentInstallName> executionOrder)
    {
        var preferredProxyName = ResolvePreparationProxyName(context);
        var normalizedContext = context;
        if (!context.HasPlannedComponentInstallers
            && !string.IsNullOrWhiteSpace(preferredProxyName)
            && !string.Equals(preferredProxyName, context.FinalDllName, StringComparison.OrdinalIgnoreCase))
        {
            normalizedContext = context with
            {
                FinalDllName = preferredProxyName
            };
        }

        if (!executionOrder.Contains(ComponentInstallName.SpecialK))
        {
            return normalizedContext;
        }

        _specialKInstaller.CleanupRootSpecialKBeforeProxyResolution(
            normalizedContext.TargetPath,
            normalizedContext.SpecialKValue,
            preferredProxyName);
        return normalizedContext;
    }

    private static string ResolvePreparationProxyName(ComponentInstallContext context)
    {
        var plannedFinal = (context.FinalDllName ?? "").Trim();
        if (context.HasPlannedComponentInstallers)
        {
            return plannedFinal;
        }

        var preferred = (context.ExecutionDescriptor.PreferredProxyDllName ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        return plannedFinal;
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

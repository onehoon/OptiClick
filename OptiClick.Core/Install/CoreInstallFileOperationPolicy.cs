using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallFileOperationPolicy
{
    private readonly CoreInstallTargetResolver _targetResolver;

    public CoreInstallFileOperationPolicy()
    {
        _targetResolver = new CoreInstallTargetResolver();
    }

    public CoreInstallFileOperationPolicy(CoreInstallTargetResolver targetResolver)
    {
        _targetResolver = targetResolver ?? new CoreInstallTargetResolver();
    }

    public IReadOnlyList<CoreInstallPlanFileOperation> ResolveFileOperations(
        CoreInstallPlanBuildInput input,
        IReadOnlyList<CoreInstallPlanComponent> components)
    {
        if (input is null || input.GameDescriptor is null)
        {
            return Array.Empty<CoreInstallPlanFileOperation>();
        }

        var targets = _targetResolver.ResolveTargets(input);
        var targetFolder = targets.TargetFolder;
        var finalProxy = targets.FinalProxyDllName;
        var operations = new List<CoreInstallPlanFileOperation>
        {
            new()
            {
                Type = CoreInstallPlanFileOperationType.BackupManagedOptiScalerDll,
                DestinationPathHint = targetFolder,
                Component = CoreInstallPlanComponentType.OptiScalerCore,
                RequiresExistingFileSnapshot = true,
                Notes = "Backup OptiScaler-managed proxy candidates before overwrite."
            },
            new()
            {
                Type = CoreInstallPlanFileOperationType.RemoveLegacyOptiScalerFile,
                DestinationPathHint = targetFolder,
                Component = CoreInstallPlanComponentType.OptiScalerCore,
                IsDestructive = true,
                RequiresExistingFileSnapshot = true,
                Notes = "Remove legacy OptiScaler compatibility files."
            },
            new()
            {
                Type = CoreInstallPlanFileOperationType.CopyPayloadTree,
                SourcePathHint = input.ArchiveReadiness.OptiScalerSourceArchive,
                DestinationPathHint = targetFolder,
                Component = CoreInstallPlanComponentType.OptiScalerCore,
                Notes = "Copy extracted OptiScaler payload tree."
            },
            new()
            {
                Type = CoreInstallPlanFileOperationType.RenameOptiScalerDll,
                SourcePathHint = "OptiScaler.dll",
                DestinationPathHint = finalProxy,
                Component = CoreInstallPlanComponentType.OptiScalerCore,
                Notes = "Rename OptiScaler.dll to final proxy DLL name."
            }
        };

        foreach (var component in components.Where(ShouldCreateComponentFileOperation))
        {
            operations.Add(new CoreInstallPlanFileOperation
            {
                Type = component.Type is CoreInstallPlanComponentType.Unreal5 or CoreInstallPlanComponentType.Fsr4
                    ? CoreInstallPlanFileOperationType.ExtractArchive
                    : CoreInstallPlanFileOperationType.CopyComponentFile,
                SourcePathHint = component.RequiredArchiveAlias,
                DestinationPathHint = component.DestinationHint,
                Component = component.Type,
                IsDestructive = component.Type is CoreInstallPlanComponentType.Unreal5,
                RequiresExistingFileSnapshot = component.Type is CoreInstallPlanComponentType.Unreal5 or CoreInstallPlanComponentType.OptiPatcher,
                Notes = "Dry-run operation hint only. No file system action is executed."
            });
        }

        return operations;
    }

    private static bool ShouldCreateComponentFileOperation(CoreInstallPlanComponent component)
    {
        return component.Enabled
            && component.Type != CoreInstallPlanComponentType.OptiScalerCore
            && component.Type != CoreInstallPlanComponentType.RtssProfile;
    }
}

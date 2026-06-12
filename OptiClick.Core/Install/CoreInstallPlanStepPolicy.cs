using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallPlanStepPolicy
{
    public IReadOnlyList<CoreInstallPlanStep> BuildSteps(bool allGateChecksPassed)
    {
        return
        [
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.ValidateGate, Completed = allGateChecksPassed },
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.PrepareArchives, Completed = allGateChecksPassed },
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.BuildComponents, Completed = true },
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.BuildFileOperations, Completed = true },
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.BuildConfigEdits, Completed = true },
            new CoreInstallPlanStep { Type = CoreInstallPlanStepType.FinalizeSummary, Completed = true }
        ];
    }
}

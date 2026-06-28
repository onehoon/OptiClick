using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public static class CoreInstallPlanReasonCodes
{
    public const string MultiGpuBlocked = CoreInstallGateReasonCodes.MultiGpuBlocked;
    public const string GpuSelectionPending = CoreInstallGateReasonCodes.GpuSelectionPending;
    public const string SheetLoading = CoreInstallGateReasonCodes.SheetLoading;
    public const string SheetNotReady = CoreInstallGateReasonCodes.SheetNotReady;
    public const string InstallInProgress = CoreInstallGateReasonCodes.InstallInProgress;
    public const string AppUpdateInProgress = CoreInstallGateReasonCodes.AppUpdateInProgress;
    public const string NoGameSelected = CoreInstallGateReasonCodes.NoGameSelected;
    public const string InstallPrecheckRunning = CoreInstallGateReasonCodes.InstallPrecheckRunning;
    public const string PrecheckIncomplete = CoreInstallGateReasonCodes.PrecheckIncomplete;
    public const string OptiScalerArchiveDownloading = CoreInstallGateReasonCodes.OptiScalerArchiveDownloading;
    public const string OptiScalerArchiveNotReady = CoreInstallGateReasonCodes.OptiScalerArchiveNotReady;
    public const string UnsupportedGpu = CoreInstallGateReasonCodes.UnsupportedGpu;
    public const string ConfirmPopupRequired = CoreInstallGateReasonCodes.ConfirmPopupRequired;
    public const string PredownloadInProgress = CoreInstallGateReasonCodes.PredownloadInProgress;
    public const string InvalidPreferredProxyName = ProxyDllNamePolicy.InvalidPreferredProxyNameErrorCode;
}

public sealed class CoreInstallPlanPolicyBuilder
{
    private readonly CoreInstallGatePolicy _gatePolicy;
    private readonly CoreInstallComponentPolicy _componentPolicy;
    private readonly CoreInstallFileOperationPolicy _fileOperationPolicy;
    private readonly CoreInstallConfigEditPolicy _configEditPolicy;
    private readonly CoreInstallTargetResolver _targetResolver;
    private readonly CoreInstallPlanSummaryPolicy _summaryPolicy;
    private readonly CoreInstallPlanStepPolicy _stepPolicy;

    public CoreInstallPlanPolicyBuilder()
        : this(
            new CoreInstallGatePolicy(),
            new CoreInstallComponentPolicy(),
            new CoreInstallFileOperationPolicy(),
            new CoreInstallConfigEditPolicy(),
            new CoreInstallTargetResolver(),
            new CoreInstallPlanSummaryPolicy(),
            new CoreInstallPlanStepPolicy())
    {
    }

    public CoreInstallPlanPolicyBuilder(CoreInstallGatePolicy gatePolicy)
        : this(
            gatePolicy,
            new CoreInstallComponentPolicy(),
            new CoreInstallFileOperationPolicy(),
            new CoreInstallConfigEditPolicy(),
            new CoreInstallTargetResolver(),
            new CoreInstallPlanSummaryPolicy(),
            new CoreInstallPlanStepPolicy())
    {
    }

    public CoreInstallPlanPolicyBuilder(
        CoreInstallGatePolicy gatePolicy,
        CoreInstallComponentPolicy componentPolicy,
        CoreInstallFileOperationPolicy fileOperationPolicy,
        CoreInstallConfigEditPolicy configEditPolicy,
        CoreInstallTargetResolver targetResolver,
        CoreInstallPlanSummaryPolicy summaryPolicy,
        CoreInstallPlanStepPolicy stepPolicy)
    {
        _gatePolicy = gatePolicy ?? new CoreInstallGatePolicy();
        _componentPolicy = componentPolicy ?? new CoreInstallComponentPolicy();
        _fileOperationPolicy = fileOperationPolicy ?? new CoreInstallFileOperationPolicy();
        _configEditPolicy = configEditPolicy ?? new CoreInstallConfigEditPolicy();
        _targetResolver = targetResolver ?? new CoreInstallTargetResolver();
        _summaryPolicy = summaryPolicy ?? new CoreInstallPlanSummaryPolicy();
        _stepPolicy = stepPolicy ?? new CoreInstallPlanStepPolicy();
    }

    public CoreInstallPlanBuildResult Build(CoreInstallPlanBuildInput input)
    {
        if (input is null)
        {
            return CoreInstallPlanBuildResult.Failure(new CoreInstallPlan(), "invalid_input");
        }

        var blockReasons = _gatePolicy.ResolveBlockReasons(input);
        var warnings = _summaryPolicy.ResolveWarnings(input.Precheck.Findings);
        var components = _componentPolicy.ResolveComponents(input);
        var fileOperations = _fileOperationPolicy.ResolveFileOperations(input, components);
        var configEdits = _configEditPolicy.ResolveConfigEdits(input.ConfigProfiles);
        var targets = _targetResolver.ResolveTargets(input);
        var steps = _stepPolicy.BuildSteps(blockReasons.Count == 0);
        var game = input.GameDescriptor;

        var plan = new CoreInstallPlan
        {
            IsAllowed = blockReasons.Count == 0,
            BlockReasons = blockReasons,
            Warnings = warnings,
            GameId = (game?.GameId ?? "").Trim(),
            GameDisplayName = targets.GameDisplayName,
            TargetFolder = targets.TargetFolder,
            MatchedExe = targets.MatchedExe,
            FinalProxyDllName = targets.FinalProxyDllName,
            ExcludeListPatterns = targets.ExcludeListPatterns,
            Components = components,
            FileOperations = fileOperations,
            ConfigEdits = configEdits,
            Steps = steps,
            Summary = _summaryPolicy.BuildSummary(targets.FinalProxyDllName, components, warnings)
        };

        return CoreInstallPlanBuildResult.Success(plan);
    }

}

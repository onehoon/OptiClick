using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Flow;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OperatingSystemSupportState = OptiClick.Infrastructure.Windows.OperatingSystemSupportState;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeShellState
{
    private readonly object _startupRemoteCatalogSnapshotGate = new();

    public OperatingSystemSupportState OperatingSystemState { get; private set; } =
        OperatingSystemSupportState.Supported("unknown");

    public bool HasEvaluatedOperatingSystemPolicy { get; private set; }

    public RuntimeContext LatestRuntimeContext { get; private set; } = new();

    public RemoteRuntimeData LatestRuntimeData { get; private set; } = RemoteRuntimeData.Empty;

    public ShellGameCatalog LatestRemoteCatalog { get; private set; } = ShellGameCatalog.Empty;

    public string LatestRemoteCatalogErrorCode { get; private set; } = "";

    public string LatestRemoteCatalogDetailErrorCode { get; private set; } = "";

    public RuntimeCatalogStartupSnapshot? StartupRemoteCatalogSnapshot { get; private set; }

    public bool IsGpuManifestRestartRequired { get; private set; }

    public bool HasShownGpuManifestRestartDialog { get; private set; }

    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; private set; } =
        ArchiveReadinessSnapshot.NotReady;

    public ModuleDownloadLinkContext ModuleDownloadLinks { get; private set; } =
        ModuleDownloadLinkContext.Empty;

    public OptiScalerVariantCatalog LatestOptiScalerVariantCatalog { get; private set; } =
        OptiScalerVariantCatalog.Empty;

    public IReadOnlyList<OptiScalerVariantSelectionOption> LatestOptiScalerVariantSelectionOptions { get; private set; } = [];

    public string EffectiveOptiScalerVariant { get; private set; } = OptiScalerVariantCatalogBuilder.StableVariant;

    public void ApplyRuntimeSummary(RuntimeSummaryStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        LatestRuntimeContext = update.RuntimeContext ?? new RuntimeContext();
    }

    public void ApplyRemoteCatalog(
        RemoteRuntimeData? runtimeData,
        ShellGameCatalog? remoteCatalog,
        ModuleDownloadLinkContext? moduleDownloadLinks,
        OptiScalerVariantCatalog? optiScalerVariantCatalog = null)
    {
        LatestRuntimeData = runtimeData ?? RemoteRuntimeData.Empty;
        LatestRemoteCatalog = remoteCatalog ?? ShellGameCatalog.Empty;
        ModuleDownloadLinks = moduleDownloadLinks ?? ModuleDownloadLinkContext.Empty;
        LatestOptiScalerVariantCatalog = optiScalerVariantCatalog ?? OptiScalerVariantCatalog.Empty;
    }

    public void ApplyOptiScalerVariantSync(OptiScalerVariantSyncResult? result)
    {
        if (result is null)
        {
            LatestOptiScalerVariantSelectionOptions = [];
            EffectiveOptiScalerVariant = OptiScalerVariantCatalogBuilder.StableVariant;
            return;
        }

        LatestOptiScalerVariantSelectionOptions = result.SelectionOptions ?? [];
        EffectiveOptiScalerVariant = string.IsNullOrWhiteSpace(result.EffectiveVariant)
            ? OptiScalerVariantCatalogBuilder.StableVariant
            : result.EffectiveVariant;
    }

    public void SetRemoteCatalogError(string? errorCode, string? detailErrorCode = "")
    {
        LatestRemoteCatalogErrorCode = (errorCode ?? "").Trim();
        LatestRemoteCatalogDetailErrorCode = (detailErrorCode ?? "").Trim();
    }

    public bool TryCaptureStartupRemoteCatalogSnapshot(
        RuntimeCatalogFlowResult? result,
        string? normalizedErrorCode)
    {
        if (result is null)
        {
            return false;
        }

        lock (_startupRemoteCatalogSnapshotGate)
        {
            if (StartupRemoteCatalogSnapshot is not null)
            {
                return false;
            }

            StartupRemoteCatalogSnapshot = new RuntimeCatalogStartupSnapshot(
                result with { Logs = [] },
                (normalizedErrorCode ?? "").Trim());
            return true;
        }
    }

    public bool TryGetStartupRemoteCatalogSnapshot(out RuntimeCatalogStartupSnapshot snapshot)
    {
        lock (_startupRemoteCatalogSnapshotGate)
        {
            if (StartupRemoteCatalogSnapshot is null)
            {
                snapshot = null!;
                return false;
            }

            snapshot = StartupRemoteCatalogSnapshot;
            return true;
        }
    }

    public void SetGpuManifestRestartRequired(bool value)
    {
        IsGpuManifestRestartRequired = value;
    }

    public void SetGpuManifestRestartDialogShown(bool value)
    {
        HasShownGpuManifestRestartDialog = value;
    }

    public void SetArchiveReadiness(ArchiveReadinessSnapshot readiness)
    {
        LatestArchiveReadiness = readiness ?? ArchiveReadinessSnapshot.NotReady;
    }

    public OperatingSystemSupportState EnsureOperatingSystemEvaluated(
        OptiClick.Infrastructure.Windows.IOperatingSystemSupportPolicy operatingSystemSupportPolicy,
        string unknownStatusCode = "unknown")
    {
        ArgumentNullException.ThrowIfNull(operatingSystemSupportPolicy);
        if (HasEvaluatedOperatingSystemPolicy)
        {
            return OperatingSystemState;
        }

        HasEvaluatedOperatingSystemPolicy = true;
        try
        {
            OperatingSystemState = operatingSystemSupportPolicy.Evaluate();
        }
        catch
        {
            OperatingSystemState = OperatingSystemSupportState.Supported((unknownStatusCode ?? "").Trim());
        }

        return OperatingSystemState;
    }
}

public sealed record RuntimeCatalogStartupSnapshot(
    RuntimeCatalogFlowResult Result,
    string NormalizedErrorCode);

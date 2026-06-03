using OptiClick.Core.Runtime;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;
using OptiClick.Wpf.Shell.Games;
using OperatingSystemSupportState = OptiClick.Infrastructure.Windows.OperatingSystemSupportState;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed class RuntimeShellState
{
    public OperatingSystemSupportState OperatingSystemState { get; private set; } =
        OperatingSystemSupportState.Supported("unknown");

    public bool HasEvaluatedOperatingSystemPolicy { get; private set; }

    public RuntimeContext LatestRuntimeContext { get; private set; } = new();

    public RemoteRuntimeData LatestRuntimeData { get; private set; } = RemoteRuntimeData.Empty;

    public ShellGameCatalog LatestRemoteCatalog { get; private set; } = ShellGameCatalog.Empty;

    public string LatestRemoteCatalogErrorCode { get; private set; } = "";

    public string LatestRemoteCatalogDetailErrorCode { get; private set; } = "";

    public ArchiveReadinessSnapshot LatestArchiveReadiness { get; private set; } =
        ArchiveReadinessSnapshot.NotReady;

    public IReadOnlyDictionary<string, object?> ModuleDownloadLinks { get; private set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public void ApplyRuntimeSummary(RuntimeSummaryStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        LatestRuntimeContext = update.RuntimeContext ?? new RuntimeContext();
    }

    public void ApplyRemoteCatalog(
        RemoteRuntimeData? runtimeData,
        ShellGameCatalog? remoteCatalog,
        IReadOnlyDictionary<string, object?>? moduleDownloadLinks)
    {
        LatestRuntimeData = runtimeData ?? RemoteRuntimeData.Empty;
        LatestRemoteCatalog = remoteCatalog ?? ShellGameCatalog.Empty;
        ModuleDownloadLinks = moduleDownloadLinks
            ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public void SetRemoteCatalogError(string? errorCode, string? detailErrorCode = "")
    {
        LatestRemoteCatalogErrorCode = (errorCode ?? "").Trim();
        LatestRemoteCatalogDetailErrorCode = (detailErrorCode ?? "").Trim();
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

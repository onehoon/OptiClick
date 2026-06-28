using OptiClick.Core.Install;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class StartupPreparationDecisionService
{
    private readonly IAppLocalDataPathProvider _localDataPathProvider;

    public StartupPreparationDecisionService(IAppLocalDataPathProvider localDataPathProvider)
    {
        _localDataPathProvider = localDataPathProvider ?? throw new ArgumentNullException(nameof(localDataPathProvider));
    }

    public StartupPreparationDecision Decide(StartupPreparationDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ShouldBlockStartupForUnsupportedOperatingSystem)
        {
            return StartupPreparationDecision.Skip();
        }

        if (StartupArchiveReadinessLocalProbe.TryBuildReadySnapshot(
                _localDataPathProvider,
                request.ModuleDownloadLinks,
                request.OptiScalerVariantCatalog,
                out var localReadiness)
            && AreRequiredStartupArchivesReady(localReadiness))
        {
            return StartupPreparationDecision.SkipWithLocalReadiness(localReadiness);
        }

        if (AreRequiredStartupArchivesReady(request.LatestArchiveReadiness)
            && StartupArchiveReadinessLocalProbe.AreStartupOverlayVariantTargetsReady(
                _localDataPathProvider,
                request.OptiScalerVariantCatalog))
        {
            return StartupPreparationDecision.Skip();
        }

        return StartupPreparationDecision.ShowOverlay();
    }

    private static bool AreRequiredStartupArchivesReady(ArchiveReadinessSnapshot readiness)
    {
        return readiness.AreAllStartupArchivesReady();
    }
}

public sealed record StartupPreparationDecisionRequest
{
    public bool ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public OptiScalerVariantCatalog OptiScalerVariantCatalog { get; init; } =
        OptiScalerVariantCatalog.Empty;
}

public sealed record StartupPreparationDecision
{
    public bool ShouldShowOverlay { get; init; }
    public ArchiveReadinessSnapshot? LocalReadiness { get; init; }

    public static StartupPreparationDecision ShowOverlay()
    {
        return new StartupPreparationDecision
        {
            ShouldShowOverlay = true
        };
    }

    public static StartupPreparationDecision Skip()
    {
        return new StartupPreparationDecision();
    }

    public static StartupPreparationDecision SkipWithLocalReadiness(ArchiveReadinessSnapshot localReadiness)
    {
        return new StartupPreparationDecision
        {
            LocalReadiness = localReadiness
        };
    }
}

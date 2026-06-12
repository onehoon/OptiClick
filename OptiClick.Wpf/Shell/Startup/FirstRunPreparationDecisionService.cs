using OptiClick.Core.Abstractions;
using OptiClick.Core.Models;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Wpf.Install.Archives;
using OptiClick.Wpf.Install.Planning;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Shell.Startup;

public sealed class FirstRunPreparationDecisionService
{
    private readonly IFirstRunStateStore _firstRunStateStore;
    private readonly IAppLocalDataPathProvider _localDataPathProvider;

    public FirstRunPreparationDecisionService(
        IFirstRunStateStore firstRunStateStore,
        IAppLocalDataPathProvider localDataPathProvider)
    {
        _firstRunStateStore = firstRunStateStore ?? throw new ArgumentNullException(nameof(firstRunStateStore));
        _localDataPathProvider = localDataPathProvider ?? throw new ArgumentNullException(nameof(localDataPathProvider));
    }

    public async Task<FirstRunPreparationDecision> DecideAsync(
        FirstRunPreparationDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ShouldBlockStartupForUnsupportedOperatingSystem)
        {
            return FirstRunPreparationDecision.Skip();
        }

        if (AreRequiredStartupArchivesReady(request.LatestArchiveReadiness))
        {
            return FirstRunPreparationDecision.Skip();
        }

        var state = await _firstRunStateStore.LoadAsync(cancellationToken);
        if (state.FirstStartupCompleted || state.ArchivePreparedOnce)
        {
            return FirstRunPreparationDecision.Skip();
        }

        if (StartupArchiveReadinessLocalProbe.TryBuildReadySnapshot(
                _localDataPathProvider,
                request.ModuleDownloadLinks,
                request.Fsr4VariantCatalog,
                out var localReadiness)
            && AreRequiredStartupArchivesReady(localReadiness))
        {
            return FirstRunPreparationDecision.SkipWithLocalReadiness(localReadiness);
        }

        return FirstRunPreparationDecision.ShowOverlay();
    }

    private static bool AreRequiredStartupArchivesReady(ArchiveReadinessSnapshot readiness)
    {
        return readiness.AreAllStartupArchivesReady();
    }
}

public sealed record FirstRunPreparationDecisionRequest
{
    public bool ShouldBlockStartupForUnsupportedOperatingSystem { get; init; }
    public required ArchiveReadinessSnapshot LatestArchiveReadiness { get; init; }
    public ModuleDownloadLinkContext ModuleDownloadLinks { get; init; } =
        ModuleDownloadLinkContext.Empty;
    public Fsr4VariantCatalog Fsr4VariantCatalog { get; init; } = Fsr4VariantCatalog.Empty;
}

public sealed record FirstRunPreparationDecision
{
    public bool ShouldShowOverlay { get; init; }
    public ArchiveReadinessSnapshot? LocalReadiness { get; init; }
    public bool ShouldSavePreparedMarker { get; init; }

    public static FirstRunPreparationDecision ShowOverlay()
    {
        return new FirstRunPreparationDecision
        {
            ShouldShowOverlay = true
        };
    }

    public static FirstRunPreparationDecision Skip()
    {
        return new FirstRunPreparationDecision();
    }

    public static FirstRunPreparationDecision SkipWithLocalReadiness(ArchiveReadinessSnapshot localReadiness)
    {
        return new FirstRunPreparationDecision
        {
            LocalReadiness = localReadiness,
            ShouldSavePreparedMarker = true
        };
    }
}

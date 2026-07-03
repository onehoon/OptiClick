using OptiClick.Wpf.Install.Flow;

namespace OptiClick.Wpf.Install.Archives;

internal static class ArchiveReadinessLogFormatter
{
    public static string FormatArchivePreparationLog(ArchiveAssetKey key, ArchivePreparationState state)
    {
        var stage = state.StageStatus ?? ArchivePreparationStageStatus.Unknown;
        return $"asset={FormatAssetKey(key)} state={FormatState(state)} source={Normalize(stage.Source, "unknown")} download={Normalize(stage.Download, "unknown")} sha={Normalize(stage.Sha, "unknown")} folder={Normalize(stage.Folder, "unknown")} json={Normalize(stage.Json, "unknown")} duration_ms={FormatDuration(stage.DurationMs)} filename={Normalize(state.Filename, "-")} error={Normalize(state.ErrorMessage, "-")}";
    }

    public static bool ShouldLogArchiveDetail(ArchivePreparationState state)
    {
        return !state.Ready
               || state.Downloading
               || !string.IsNullOrWhiteSpace(state.ErrorMessage);
    }

    public static IEnumerable<InstallFlowLogEntry> FormatOptiPatcherInjectionLogs(
        OptiScalerPayloadOptiPatcherInjectionResult injection)
    {
        var injected = injection.Targets.Count(static target => target.Injected);
        var existing = injection.Targets.Count(static target => target.UsedExisting);
        yield return InstallFlowLogEntryFactory.Info(
            "archive",
            $"optipatcher injection ready={FormatBool(injection.IsReady)} targets={injection.Targets.Count} injected={injected} existing={existing} source={Normalize(injection.SourcePath, "-")} source_error={Normalize(injection.SourceErrorCode, "-")}");

        foreach (var target in injection.Targets.Where(static target => !target.Ready))
        {
            yield return InstallFlowLogEntryFactory.Warning(
                "archive",
                $"optipatcher injection target not_ready variant={Normalize(target.Variant, "-")} cache_entry={Normalize(target.CacheEntryName, "-")} error={Normalize(target.ErrorCode, "-")}");
        }
    }

    private static string FormatAssetKey(ArchiveAssetKey key)
    {
        if (StartupArchiveAssetDefinitions.TryGet(key, out var definition))
        {
            return definition.RuntimeDataKey;
        }

        return key switch
        {
            ArchiveAssetKey.OptiScaler => ArchiveAssetRuntimeDataKeys.OptiScaler,
            ArchiveAssetKey.OptiPatcher => ArchiveAssetRuntimeDataKeys.OptiPatcher,
            _ => key.ToString().ToLowerInvariant()
        };
    }

    private static string FormatState(ArchivePreparationState state)
    {
        if (state.Downloading)
        {
            return "Downloading";
        }

        if (state.Ready)
        {
            return "Ready";
        }

        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            return "Failed";
        }

        return string.IsNullOrWhiteSpace(state.ArchivePath) ? "MissingSource" : "NotReady";
    }

    private static string FormatDuration(long durationMs)
    {
        return durationMs < 0 ? "-" : durationMs.ToString();
    }

    private static string FormatBool(bool value)
    {
        return value.ToString().ToLowerInvariant();
    }

    private static string Normalize(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

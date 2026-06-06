using OptiClick.Wpf.Install.Planning;

namespace OptiClick.Wpf.Install.Precheck;

public sealed record ModBinaryState
{
    public bool Detected { get; init; }
    public IReadOnlyList<string> DllNames { get; init; } = Array.Empty<string>();
}

public sealed record ModFilePresenceState
{
    public bool Detected { get; init; }
    public IReadOnlyList<string> RelativePaths { get; init; } = Array.Empty<string>();
}

public sealed record ModPrecheckState
{
    public static readonly ModPrecheckState Empty = new()
    {
        ReShade = new ModBinaryState(),
        SpecialK = new ModBinaryState(),
        UltimateAsiLoader = new ModBinaryState(),
        RenoDx = new ModFilePresenceState(),
        LennyModLoader = new ModFilePresenceState(),
        ScriptHookRdr2 = new ModFilePresenceState()
    };

    public ModBinaryState ReShade { get; init; } = new();
    public ModBinaryState SpecialK { get; init; } = new();
    public ModBinaryState UltimateAsiLoader { get; init; } = new();
    public ModFilePresenceState RenoDx { get; init; } = new();
    public ModFilePresenceState LennyModLoader { get; init; } = new();
    public ModFilePresenceState ScriptHookRdr2 { get; init; } = new();
}

public sealed record ModConflictFinding
{
    public string Kind { get; init; } = "";
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record InstallPrecheckResult
{
    public bool Ok { get; init; }
    public string ResolvedDllName { get; init; } = "";
    public string RawErrorMessage { get; init; } = "";
    public IReadOnlyList<ModConflictFinding> ConflictFindings { get; init; } = Array.Empty<ModConflictFinding>();
    public IReadOnlyList<ModConflictFinding>? NoticeFindings { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyDictionary<string, string> ErrorContext { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record InstallPrecheckRequest
{
    public string TargetPath { get; init; } = "";
    public string PreferredDllName { get; init; } = "";
    public Shell.Games.ShellGameCardModel? Game { get; init; }
}

public sealed record InstallSelectionUiState
{
    public bool PopupConfirmed { get; init; }
    public bool PrecheckRunning { get; init; }
    public bool PrecheckOk { get; init; }
    public string PrecheckError { get; init; } = "";
    public string PrecheckDllName { get; init; } = "";
}

public sealed record InstallSelectionPrecheckOutcome
{
    public bool Ok { get; init; }
    public string Error { get; init; } = "";
    public string ResolvedDllName { get; init; } = "";
    public string PopupMessage { get; init; } = "";
    public string ModNoticeMessage { get; init; } = "";
    public IReadOnlyList<string> ModNoticeBulletItems { get; init; } = Array.Empty<string>();
    public string ModNoticeFooterText { get; init; } = "";
}

public enum PopupRequestType
{
    Precheck,
    ModNotice,
    Selection
}

public sealed record PopupRequest
{
    public PopupRequestType Type { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<string> BulletItems { get; init; } = Array.Empty<string>();
    public string FooterText { get; init; } = "";
}

public sealed record InstallSelectionPrecheckFlowResult
{
    public InstallSelectionUiState State { get; init; } = new();
    public IReadOnlyList<PopupRequest> PopupRequests { get; init; } = Array.Empty<PopupRequest>();
    public bool ConfirmedImmediately { get; init; }
    public bool ConfirmAfterPopupChain { get; init; }
}

public static class InstallPrecheckSnapshotMapper
{
    public static InstallPrecheckSnapshot ToSnapshot(InstallPrecheckResult result)
    {
        var state = result.Ok ? InstallPrecheckState.Passed : InstallPrecheckState.Failed;
        return new InstallPrecheckSnapshot
        {
            State = state,
            ErrorText = result.RawErrorMessage,
            ResolvedDllName = result.ResolvedDllName,
            Findings = result.ConflictFindings.Select(finding => new InstallPrecheckFinding
            {
                Kind = finding.Kind,
                Message = BuildFindingMessage(finding),
                Evidence = finding.Evidence
                    .Select(value => (value ?? "").Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                IsBlocking = string.Equals(result.ErrorCode, InstallPrecheckErrorCodes.BlockedMods, StringComparison.OrdinalIgnoreCase)
            }).ToArray()
        };
    }

    private static string BuildFindingMessage(ModConflictFinding finding)
    {
        var evidence = string.Join(", ", finding.Evidence);
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return finding.Kind;
        }

        return $"{finding.Kind}: {evidence}";
    }
}

using OptiClick.Wpf.Shell.Games;

namespace OptiClick.Wpf.Install.Precheck;

public sealed class ModConflictFindingBuilder
{
    public IReadOnlyList<ModConflictFinding> BuildFindings(ModPrecheckState state)
    {
        var findings = new List<ModConflictFinding>();

        var reShade = BuildFinding(ModConflictKinds.ReShade, state.ReShade.DllNames);
        if (reShade is not null)
        {
            findings.Add(reShade);
        }

        var specialK = BuildFinding(ModConflictKinds.SpecialK, state.SpecialK.DllNames);
        if (specialK is not null)
        {
            findings.Add(specialK);
        }

        var ual = BuildFinding(ModConflictKinds.UltimateAsiLoader, state.UltimateAsiLoader.DllNames);
        if (ual is not null)
        {
            findings.Add(ual);
        }

        var renoDx = BuildFinding(ModConflictKinds.RenoDx, state.RenoDx.AddonPaths);
        if (renoDx is not null)
        {
            findings.Add(renoDx);
        }

        return findings;
    }

    public IReadOnlyList<ModConflictFinding> SuppressManagedSpecialKFindings(
        IEnumerable<ModConflictFinding> findings,
        ShellGameCardModel? game)
    {
        if (!IsSpecialKManagedByInstaller(game))
        {
            return findings.ToArray();
        }

        return findings
            .Where(finding => !string.Equals((finding.Kind ?? "").Trim(), ModConflictKinds.SpecialK, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public bool IsSpecialKManagedByInstaller(ShellGameCardModel? game)
    {
        return !string.IsNullOrWhiteSpace(ShellGameInstallMetadataResolver.GetSpecialK(game));
    }

    private static ModConflictFinding? BuildFinding(string kind, IEnumerable<string> evidence)
    {
        var normalized = ModPrecheckScanner.NormalizeUniqueStrings(evidence);
        if (normalized.Count == 0)
        {
            return null;
        }

        return new ModConflictFinding
        {
            Kind = kind,
            Evidence = normalized
        };
    }
}

public sealed record ModConflictNoticeDecision
{
    public ModConflictNoticeMode Mode { get; init; }
    public string NoticeText { get; init; } = "";
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
}

public sealed class ModConflictNoticeBuilder
{
    public ModConflictNoticeDecision Build(IEnumerable<ModConflictFinding> findings, bool useKorean = false)
    {
        var normalizedFindings = findings?.ToArray() ?? Array.Empty<ModConflictFinding>();
        if (normalizedFindings.Length == 0)
        {
            return new ModConflictNoticeDecision
            {
                Mode = ModConflictNoticeMode.None
            };
        }

        if (normalizedFindings.All(finding => string.Equals(finding.Kind, ModConflictKinds.UltimateAsiLoader, StringComparison.OrdinalIgnoreCase)))
        {
            return new ModConflictNoticeDecision
            {
                Mode = ModConflictNoticeMode.EmptyForUltimateAsiOnly
            };
        }

        if (normalizedFindings.All(finding => string.Equals((finding.Kind ?? "").Trim(), ModConflictKinds.ReFrameworkLegacy, StringComparison.OrdinalIgnoreCase)))
        {
            var lines = normalizedFindings.Select(finding => FormatFinding(finding, useKorean)).ToArray();
            return new ModConflictNoticeDecision
            {
                Mode = ModConflictNoticeMode.ReFrameworkLegacyOnly,
                Lines = lines,
                NoticeText = string.Join("\n\n", lines).Trim()
            };
        }

        var genericLines = normalizedFindings.Select(finding => FormatFinding(finding, useKorean)).ToArray();
        var noticeText = BuildGenericNotice(genericLines, useKorean);
        return new ModConflictNoticeDecision
        {
            Mode = ModConflictNoticeMode.GenericModConflict,
            Lines = genericLines,
            NoticeText = noticeText
        };
    }

    private static string BuildGenericNotice(IReadOnlyList<string> lines, bool useKorean)
    {
        if (lines.Count == 0)
        {
            return "";
        }

        var header = useKorean
            ? "[RED]다른 MOD가 감지되었습니다.\n감지된 항목에 따라 일부는 자동으로 처리될 수 있지만,\n일부 MOD는 게임 실행 실패 또는 오동작을 유발할 수 있습니다.[END]"
            : "[RED]Other MODs were detected.\nSome detected MODs may be handled automatically,\nbut certain MODs can prevent the game from launching or cause unexpected behavior.[END]";
        var footer = useKorean
            ? "감지된 항목을 확인한 뒤 설치를 계속하려면 설치 버튼을 눌러 진행해 주세요."
            : "After reviewing the detected MODs, press the Install button in the main window to continue.";

        return string.Join(
            "\n",
            new[]
            {
                header,
                "",
                string.Join("\n", lines.Select(static line => $"[INDENT][DOT]{line}")),
                "",
                footer
            }).Trim();
    }

    private static string FormatFinding(ModConflictFinding finding, bool useKorean)
    {
        var evidence = string.Join(", ", finding.Evidence);
        var kind = (finding.Kind ?? "").Trim().ToLowerInvariant();

        if (kind == ModConflictKinds.ReFrameworkLegacy)
        {
            var destination = finding.Context.TryGetValue("destination", out var destinationValue) && !string.IsNullOrWhiteSpace(destinationValue)
                ? destinationValue
                : "REFramework target DLL";
            return useKorean
                ? $"게임 폴더에 기존 REFramework 파일(dinput8.dll)이 감지되었습니다.\n\n정상적인 설치 및 동작을 위해 dinput8.dll을 삭제한 뒤 설치해 주세요.\n\nREFramework는 {destination}로 재설치됩니다."
                : $"An existing REFramework file (dinput8.dll) was detected in the game folder.\n\nFor proper installation and operation, delete dinput8.dll before installing.\n\nREFramework will be reinstalled as {destination}.";
        }

        return kind switch
        {
            ModConflictKinds.ReShade => useKorean
                ? $"ReShade: {evidence}\n\nReShade({evidence})와의 호환성을 위해 OptiScaler는 다른 이름으로 설치됩니다."
                : $"ReShade: {evidence}\n\nFor ReShade ({evidence}) compatibility, OptiScaler will be installed with a different name.",
            ModConflictKinds.SpecialK => $"Special K: {evidence}",
            ModConflictKinds.UltimateAsiLoader => $"Ultimate ASI Loader: {evidence}",
            ModConflictKinds.RenoDx => $"RenoDX: {evidence}",
            _ => useKorean
                ? $"MOD: {evidence}"
                : $"MOD: {evidence}"
        };
    }

}

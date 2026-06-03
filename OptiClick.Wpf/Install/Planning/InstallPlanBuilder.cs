using OptiClick.Wpf.Shell.Games.Actions;
using OptiClick.Wpf.Shell.Games;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.Install.Execution;
using OptiClick.Wpf.Install.Precheck;
using OptiClick.Wpf.Shell.RuntimeData;

namespace OptiClick.Wpf.Install.Planning;

public interface IInstallPlanBuilder
{
    InstallPlanBuildResult Build(InstallPlanBuildInput input);
}

public sealed class InstallPlanBuilder : IInstallPlanBuilder
{
    private sealed record OptionalComponentRule(
        InstallPlanComponentType Type,
        Func<ShellGameCardModel, InstallPlanBuildInput, bool> IsEnabled,
        string SourceKind,
        Func<ShellGameCardModel, string> DestinationHintFactory,
        string RequiredAlias,
        string SkipReason);

    private sealed record ConfigEditPolicy(
        bool CreatesFileAllowed,
        bool CreatesMissingPathAllowed,
        bool AllowsAddMissingKey,
        bool AllowsAddMissingSection,
        bool UsesValuePathHint,
        string Notes);

    private static readonly IReadOnlyList<OptionalComponentRule> OptionalComponentRules =
    [
        new(
            InstallPlanComponentType.OptiPatcher,
            static (game, _) => ShellGameInstallMetadataResolver.GetOptiPatcher(game),
            "archive",
            static _ => "plugins/OptiPatcher.asi",
            "optipatcher",
            "optipatcher_not_requested"),
        new(
            InstallPlanComponentType.REFramework,
            static (game, _) => !string.IsNullOrWhiteSpace(ShellGameInstallMetadataResolver.GetReFrameworkUrl(game)),
            "archive",
            static game =>
            {
                var value = ShellGameInstallMetadataResolver.GetReFrameworkUrl(game);
                return string.IsNullOrWhiteSpace(value) ? "dinput8.dll" : value;
            },
            "reframework",
            "reframework_not_requested"),
        new(
            InstallPlanComponentType.SpecialK,
            static (game, _) => !string.IsNullOrWhiteSpace(ShellGameInstallMetadataResolver.GetSpecialK(game)),
            "archive",
            static game =>
            {
                var value = ShellGameInstallMetadataResolver.GetSpecialK(game);
                return string.IsNullOrWhiteSpace(value) ? "plugins" : value;
            },
            "specialk",
            "specialk_not_requested"),
        new(
            InstallPlanComponentType.UltimateAsiLoader,
            static (game, _) => ShellGameInstallMetadataResolver.GetUltimateAsiLoader(game),
            "archive",
            static _ => "dinput8.dll",
            "ual",
            "ual_not_requested"),
        new(
            InstallPlanComponentType.Unreal5,
            static (game, _) => ShellGameInstallMetadataResolver.GetUnreal5(game),
            "archive",
            static _ => "game_root",
            "unreal5",
            "unreal5_not_requested"),
        new(
            InstallPlanComponentType.ExtraBundle,
            static (game, _) => !string.IsNullOrWhiteSpace(ShellGameInstallMetadataResolver.GetExtraBundle(game)),
            "archive",
            static _ => "game_root",
            "extra_bundle",
            "extra_bundle_not_requested"),
        new(
            InstallPlanComponentType.Fsr4,
            static (_, input) => input.IsFsr4Required,
            "archive",
            static _ => "game_root",
            "fsr4",
            "fsr4_not_required"),
        new(
            InstallPlanComponentType.RtssProfile,
            static (game, _) => ShellGameInstallMetadataResolver.GetRtssOverlay(game),
            "profile",
            static _ => "rtss_profile",
            "rtss",
            "rtss_not_requested")
    ];

    private static readonly IReadOnlyDictionary<InstallPlanConfigEditType, ConfigEditPolicy> ConfigEditPolicies =
        new Dictionary<InstallPlanConfigEditType, ConfigEditPolicy>
        {
            [InstallPlanConfigEditType.GameIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: true,
                AllowsAddMissingKey: true,
                AllowsAddMissingSection: false,
                UsesValuePathHint: false,
                Notes: "Upsert missing keys only. Missing sections are not created."),
            [InstallPlanConfigEditType.GameUnrealIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: false,
                AllowsAddMissingKey: false,
                AllowsAddMissingSection: false,
                UsesValuePathHint: true,
                Notes: "Missing value_path is skipped. No struct path creation."),
            [InstallPlanConfigEditType.GameJsonProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: false,
                AllowsAddMissingKey: false,
                AllowsAddMissingSection: false,
                UsesValuePathHint: true,
                Notes: "JSON pointer must already exist. Missing paths are skipped."),
            [InstallPlanConfigEditType.EngineIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: true,
                CreatesMissingPathAllowed: true,
                AllowsAddMissingKey: true,
                AllowsAddMissingSection: true,
                UsesValuePathHint: false,
                Notes: "Engine.ini can be created and set read-only after apply."),
            [InstallPlanConfigEditType.RegistryProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: true,
                AllowsAddMissingKey: true,
                AllowsAddMissingSection: true,
                UsesValuePathHint: false,
                Notes: "Registry profile rows are optional best-effort.")
        };

    private static readonly ConfigEditPolicy DefaultConfigEditPolicy = new(
        CreatesFileAllowed: false,
        CreatesMissingPathAllowed: false,
        AllowsAddMissingKey: false,
        AllowsAddMissingSection: false,
        UsesValuePathHint: true,
        Notes: "");

    public InstallPlanBuildResult Build(InstallPlanBuildInput input)
    {
        if (input is null)
        {
            return InstallPlanBuildResult.Failure(new InstallPlan(), "invalid_input");
        }

        var blockReasons = ResolveBlockReasons(input);
        var warnings = BuildWarnings(input.Precheck.Findings);
        var components = BuildComponents(input);
        var fileOperations = BuildFileOperations(input, components);
        var configEdits = BuildConfigEdits(input.ConfigProfiles);
        var steps = BuildSteps(blockReasons.Count == 0);

        var targetFolder = ResolveTargetFolder(input);
        var matchedExe = ResolveMatchedExe(input);
        var finalProxyDllName = ResolveFinalProxyDllName(input);
        var profileRows = ResolveProfileRows(input.SelectedGame);

        var plan = new InstallPlan
        {
            IsAllowed = blockReasons.Count == 0,
            BlockReasons = blockReasons,
            Warnings = warnings,
            GameId = (input.SelectedGame?.GameId ?? "").Trim(),
            GameDisplayName = ResolveGameDisplayName(input),
            TargetFolder = targetFolder,
            MatchedExe = matchedExe,
            FinalProxyDllName = finalProxyDllName,
            ExcludeListPatterns = ResolveExcludeListPatterns(input.SelectedGame),
            Components = components,
            FileOperations = fileOperations,
            ConfigEdits = configEdits,
            Steps = steps,
            Summary = BuildSummary(finalProxyDllName, components, warnings),
            ProfileRows = profileRows
        };

        return InstallPlanBuildResult.Success(plan);
    }

    private static IReadOnlyList<InstallPlanBlockReason> ResolveBlockReasons(InstallPlanBuildInput input)
    {
        var reasons = new List<InstallPlanBlockReason>();
        if (input.IsMultiGpuBlocked)
        {
            reasons.Add(Block(InstallPlanReasonCodes.MultiGpuBlocked));
        }

        if (input.IsGpuSelectionPending)
        {
            reasons.Add(Block(InstallPlanReasonCodes.GpuSelectionPending));
        }

        if (input.IsSheetLoading)
        {
            reasons.Add(Block(InstallPlanReasonCodes.SheetLoading));
        }

        if (!input.IsSheetReady)
        {
            reasons.Add(Block(InstallPlanReasonCodes.SheetNotReady));
        }

        if (input.IsInstallInProgress)
        {
            reasons.Add(Block(InstallPlanReasonCodes.InstallInProgress));
        }

        if (input.IsAppUpdateInProgress)
        {
            reasons.Add(Block(InstallPlanReasonCodes.AppUpdateInProgress));
        }

        if (input.SelectedGame is null)
        {
            reasons.Add(Block(InstallPlanReasonCodes.NoGameSelected));
        }
        else if (!ProxyDllNameResolver.TryResolveProfilePreferredStart(
                     ShellGameInstallMetadataResolver.GetOptiScalerDllName(input.SelectedGame),
                     out _,
                     out var preferredError))
        {
            reasons.Add(Block(preferredError));
        }

        if (input.Precheck.State == InstallPrecheckState.Running)
        {
            reasons.Add(Block(InstallPlanReasonCodes.InstallPrecheckRunning));
        }

        if (input.Precheck.State != InstallPrecheckState.Passed || string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            reasons.Add(Block(InstallPlanReasonCodes.PrecheckIncomplete, input.Precheck.ErrorText));
        }

        if (input.ArchiveReadiness.OptiScalerState == ArchiveReadinessState.Downloading)
        {
            reasons.Add(Block(InstallPlanReasonCodes.OptiScalerArchiveDownloading));
        }

        if (input.ArchiveReadiness.OptiScalerState != ArchiveReadinessState.Ready
            || string.IsNullOrWhiteSpace(input.ArchiveReadiness.OptiScalerSourceArchive))
        {
            reasons.Add(Block(InstallPlanReasonCodes.OptiScalerArchiveNotReady));
        }

        if (input.IsFsr4Required && input.ArchiveReadiness.Fsr4State != ArchiveReadinessState.Ready)
        {
            reasons.Add(Block(InstallPlanReasonCodes.Fsr4NotReady));
        }

        if (IsUnsupportedGpu(input))
        {
            reasons.Add(Block(InstallPlanReasonCodes.UnsupportedGpu));
        }

        if (!input.IsSelectionPopupConfirmed)
        {
            reasons.Add(Block(InstallPlanReasonCodes.ConfirmPopupRequired));
        }

        if (input.IsPredownloadInProgress)
        {
            reasons.Add(Block(InstallPlanReasonCodes.PredownloadInProgress));
        }

        if (input.IsWriteProbeFailed)
        {
            reasons.Add(Block(InstallPlanReasonCodes.WriteProbeFailed));
        }

        return reasons;
    }

    private static bool IsUnsupportedGpu(InstallPlanBuildInput input)
    {
        if (string.Equals(input.ActionAvailability.ReasonCode, ShellGameActionReasonCodes.UnsupportedGpu, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (input.MatchResult?.Status == ShellGameMatchStatus.UnsupportedGpu)
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<InstallPlanWarning> BuildWarnings(IReadOnlyList<InstallPrecheckFinding> findings)
    {
        if (findings is null || findings.Count == 0)
        {
            return Array.Empty<InstallPlanWarning>();
        }

        var warnings = new List<InstallPlanWarning>(findings.Count);
        foreach (var finding in findings)
        {
            var code = (finding.Kind ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            warnings.Add(new InstallPlanWarning
            {
                Code = code,
                Detail = (finding.Message ?? "").Trim(),
                IsBlockingCandidate = finding.IsBlocking
            });
        }

        return warnings;
    }

    private static IReadOnlyList<InstallPlanComponent> BuildComponents(InstallPlanBuildInput input)
    {
        var game = input.SelectedGame;
        if (game is null)
        {
            return Array.Empty<InstallPlanComponent>();
        }

        var components = new List<InstallPlanComponent>
        {
            Component(InstallPlanComponentType.OptiScalerCore, enabled: true, sourceKind: "archive", destinationHint: "game_root", requiredAlias: "optiscaler")
        };

        foreach (var rule in OptionalComponentRules)
        {
            components.Add(OptionalComponent(
                rule.Type,
                rule.IsEnabled(game, input),
                rule.SourceKind,
                rule.DestinationHintFactory(game),
                rule.RequiredAlias,
                rule.SkipReason));
        }

        return components;
    }

    private static IReadOnlyList<InstallPlanFileOperation> BuildFileOperations(
        InstallPlanBuildInput input,
        IReadOnlyList<InstallPlanComponent> components)
    {
        if (input.SelectedGame is null)
        {
            return Array.Empty<InstallPlanFileOperation>();
        }

        var targetFolder = ResolveTargetFolder(input);
        var finalProxy = ResolveFinalProxyDllName(input);
        var operations = new List<InstallPlanFileOperation>
        {
            new()
            {
                Type = InstallPlanFileOperationType.BackupManagedOptiScalerDll,
                DestinationPathHint = targetFolder,
                Component = InstallPlanComponentType.OptiScalerCore,
                RequiresExistingFileSnapshot = true,
                Notes = "Backup OptiScaler-managed proxy candidates before overwrite."
            },
            new()
            {
                Type = InstallPlanFileOperationType.RemoveLegacyOptiScalerFile,
                DestinationPathHint = targetFolder,
                Component = InstallPlanComponentType.OptiScalerCore,
                IsDestructive = true,
                RequiresExistingFileSnapshot = true,
                Notes = "Remove legacy OptiScaler compatibility files."
            },
            new()
            {
                Type = InstallPlanFileOperationType.CopyPayloadTree,
                SourcePathHint = input.ArchiveReadiness.OptiScalerSourceArchive,
                DestinationPathHint = targetFolder,
                Component = InstallPlanComponentType.OptiScalerCore,
                Notes = "Copy extracted OptiScaler payload tree."
            },
            new()
            {
                Type = InstallPlanFileOperationType.RenameOptiScalerDll,
                SourcePathHint = "OptiScaler.dll",
                DestinationPathHint = finalProxy,
                Component = InstallPlanComponentType.OptiScalerCore,
                Notes = "Rename OptiScaler.dll to final proxy DLL name."
            }
        };

        foreach (var component in components.Where(static component => component.Type != InstallPlanComponentType.OptiScalerCore && component.Enabled))
        {
            operations.Add(new InstallPlanFileOperation
            {
                Type = component.Type is InstallPlanComponentType.Unreal5 or InstallPlanComponentType.Fsr4
                    ? InstallPlanFileOperationType.ExtractArchive
                    : InstallPlanFileOperationType.CopyComponentFile,
                SourcePathHint = component.RequiredArchiveAlias,
                DestinationPathHint = component.DestinationHint,
                Component = component.Type,
                IsDestructive = component.Type is InstallPlanComponentType.Unreal5,
                RequiresExistingFileSnapshot = component.Type is InstallPlanComponentType.Unreal5 or InstallPlanComponentType.OptiPatcher,
                Notes = "Dry-run operation hint only. No file system action is executed."
            });
        }

        return operations;
    }

    private static IReadOnlyList<InstallPlanConfigEdit> BuildConfigEdits(IReadOnlyList<InstallConfigProfileHint> hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return Array.Empty<InstallPlanConfigEdit>();
        }

        var edits = new List<InstallPlanConfigEdit>(hints.Count);
        foreach (var hint in hints)
        {
            edits.Add(CreateConfigEdit(hint));
        }

        return edits;
    }

    private static InstallPlanConfigEdit CreateConfigEdit(InstallConfigProfileHint hint)
    {
        var policy = ConfigEditPolicies.TryGetValue(hint.Type, out var configured)
            ? configured
            : DefaultConfigEditPolicy;

        return new InstallPlanConfigEdit
        {
            Type = hint.Type,
            TargetPathHint = hint.TargetPathHint,
            KeyHint = hint.KeyHint,
            ValuePathHint = policy.UsesValuePathHint ? hint.ValuePathHint : "",
            CreatesFileAllowed = policy.CreatesFileAllowed,
            CreatesMissingPathAllowed = policy.CreatesMissingPathAllowed,
            AllowsAddMissingKey = policy.AllowsAddMissingKey,
            AllowsAddMissingSection = policy.AllowsAddMissingSection,
            BestEffort = true,
            Notes = policy.Notes
        };
    }

    private static IReadOnlyList<InstallPlanStep> BuildSteps(bool allGateChecksPassed)
    {
        return
        [
            new InstallPlanStep { Type = InstallPlanStepType.ValidateGate, Completed = allGateChecksPassed },
            new InstallPlanStep { Type = InstallPlanStepType.PrepareArchives, Completed = allGateChecksPassed },
            new InstallPlanStep { Type = InstallPlanStepType.BuildComponents, Completed = true },
            new InstallPlanStep { Type = InstallPlanStepType.BuildFileOperations, Completed = true },
            new InstallPlanStep { Type = InstallPlanStepType.BuildConfigEdits, Completed = true },
            new InstallPlanStep { Type = InstallPlanStepType.FinalizeSummary, Completed = true }
        ];
    }

    private static InstallPlanSummary BuildSummary(
        string finalProxyDllName,
        IReadOnlyList<InstallPlanComponent> components,
        IReadOnlyList<InstallPlanWarning> warnings)
    {
        return new InstallPlanSummary
        {
            OptiScalerTargetDll = finalProxyDllName,
            SelectedComponents = components
                .Where(static component => component.Enabled)
                .Select(static component => component.Type.ToString())
                .ToArray(),
            WarningCodes = warnings.Select(static warning => warning.Code).ToArray(),
            Notes =
            [
                "Dry-run plan only. No filesystem/network side-effects are executed."
            ]
        };
    }

    private static string ResolveTargetFolder(InstallPlanBuildInput input)
    {
        var targetCandidate = string.IsNullOrWhiteSpace(input.MatchResult?.FolderPath)
            ? input.TargetFolderHint
            : input.MatchResult?.FolderPath;
        return InstallTargetPathNormalizer.NormalizeTargetDirectory(targetCandidate);
    }

    private static string ResolveMatchedExe(InstallPlanBuildInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.MatchResult?.MatchedExe))
        {
            return input.MatchResult.MatchedExe.Trim();
        }

        if (!string.IsNullOrWhiteSpace(input.MatchedExeHint))
        {
            return input.MatchedExeHint.Trim();
        }

        return (input.SelectedGame?.MatchExe ?? "").Trim();
    }

    private static string ResolveFinalProxyDllName(InstallPlanBuildInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.Precheck.ResolvedDllName))
        {
            return input.Precheck.ResolvedDllName.Trim();
        }

        if (ProxyDllNameResolver.TryResolveProfilePreferredStart(
                ShellGameInstallMetadataResolver.GetOptiScalerDllName(input.SelectedGame),
                out var preferredStart,
                out _))
        {
            return preferredStart;
        }

        return "";
    }

    private static AttachedRuntimeProfileRows ResolveProfileRows(ShellGameCardModel? selectedGame)
    {
        var metadata = selectedGame?.InstallMetadata;
        if (metadata is null)
        {
            return AttachedRuntimeProfileRows.Empty;
        }

        return new AttachedRuntimeProfileRows
        {
            GameIniProfileRows = metadata.GameIniProfileRows,
            GameUnrealIniProfileRows = metadata.GameUnrealIniProfileRows,
            EngineIniProfileRows = metadata.EngineIniProfileRows,
            GameXmlProfileRows = metadata.GameXmlProfileRows,
            RegistryProfileRows = metadata.RegistryProfileRows,
            GameJsonProfileRows = metadata.GameJsonProfileRows
        };
    }

    private static IReadOnlyList<string> ResolveExcludeListPatterns(ShellGameCardModel? game)
    {
        if (game?.ExcludeListPatterns?.Count > 0)
        {
            return ExcludeListPatternParser.Normalize(game.ExcludeListPatterns);
        }

        var raw = (game?.ExcludeListRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return ExcludeListPatternParser.Parse(raw);
    }

    private static string ResolveGameDisplayName(InstallPlanBuildInput input)
    {
        var game = input.SelectedGame;
        if (game is null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(game.DisplayName))
        {
            return game.DisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(game.GameNameEn))
        {
            return game.GameNameEn.Trim();
        }

        if (!string.IsNullOrWhiteSpace(game.GameNameKr))
        {
            return game.GameNameKr.Trim();
        }

        return (game.GameId ?? "").Trim();
    }

    private static InstallPlanBlockReason Block(string code, string detail = "")
    {
        return new InstallPlanBlockReason
        {
            Code = code,
            Detail = (detail ?? "").Trim()
        };
    }

    private static InstallPlanComponent Component(
        InstallPlanComponentType type,
        bool enabled,
        string sourceKind,
        string destinationHint,
        string requiredAlias)
    {
        return new InstallPlanComponent
        {
            Type = type,
            Enabled = enabled,
            SourceKind = sourceKind,
            DestinationHint = destinationHint,
            RequiredArchiveAlias = requiredAlias
        };
    }

    private static InstallPlanComponent OptionalComponent(
        InstallPlanComponentType type,
        bool enabled,
        string sourceKind,
        string destinationHint,
        string requiredAlias,
        string skipReason)
    {
        return new InstallPlanComponent
        {
            Type = type,
            Enabled = enabled,
            SkipReason = enabled ? "" : skipReason,
            SourceKind = sourceKind,
            DestinationHint = destinationHint,
            RequiredArchiveAlias = requiredAlias
        };
    }
}

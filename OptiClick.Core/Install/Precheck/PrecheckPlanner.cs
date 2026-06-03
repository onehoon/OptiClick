using OptiClick.Core.Models;

namespace OptiClick.Core.Install.Precheck;

public sealed class PrecheckPlanner
{
    public PrecheckResult Evaluate(GameEntry game, ExistingFileSnapshot snapshot)
    {
        var findings = new List<PrecheckFinding>();
        var warnings = new List<string>();
        var ualNames = new List<string>();

        foreach (var file in snapshot.Files.Where(file => file.Exists))
        {
            var type = ToFindingType(file.OwnerKind);
            if (type == PrecheckFindingType.Unknown)
            {
                continue;
            }

            if (ShouldSuppressFinding(type, game))
            {
                continue;
            }

            var warning = BuildWarning(type, game);
            findings.Add(new PrecheckFinding
            {
                Type = type,
                RelativePath = file.RelativePath,
                OwnerKind = file.OwnerKind,
                Warning = warning
            });

            if (file.OwnerKind == DllOwnerKind.UltimateAsiLoader)
            {
                ualNames.Add(Path.GetFileName(file.RelativePath));
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings.Add(warning);
            }
        }

        AddLegacyReFrameworkFinding(game, snapshot, findings, warnings);
        var blockingFindingExists = findings.Any(IsBlockingFinding);

        return new PrecheckResult
        {
            Ok = !blockingFindingExists,
            Findings = findings,
            UalDetectedNames = ualNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private static PrecheckFindingType ToFindingType(DllOwnerKind ownerKind)
    {
        return ownerKind switch
        {
            DllOwnerKind.OptiScaler => PrecheckFindingType.OptiScalerManaged,
            DllOwnerKind.ReShade => PrecheckFindingType.ReShade,
            DllOwnerKind.SpecialK => PrecheckFindingType.SpecialK,
            DllOwnerKind.UltimateAsiLoader => PrecheckFindingType.UltimateAsiLoader,
            DllOwnerKind.RenoDx => PrecheckFindingType.RenoDx,
            _ => PrecheckFindingType.Unknown
        };
    }

    private static bool ShouldSuppressFinding(PrecheckFindingType type, GameEntry game)
    {
        return type == PrecheckFindingType.SpecialK && !string.IsNullOrWhiteSpace(game.SpecialK);
    }

    private static bool IsBlockingFinding(PrecheckFinding finding)
    {
        return finding.Type is PrecheckFindingType.ReShade
            or PrecheckFindingType.RenoDx
            or PrecheckFindingType.LegacyReFramework;
    }

    private static string BuildWarning(PrecheckFindingType type, GameEntry game)
    {
        if (type == PrecheckFindingType.UltimateAsiLoader)
        {
            return "";
        }

        return type switch
        {
            PrecheckFindingType.ReShade => PrecheckWarningCodes.ReShadeDetected,
            PrecheckFindingType.SpecialK => PrecheckWarningCodes.SpecialKDetected,
            PrecheckFindingType.RenoDx => PrecheckWarningCodes.RenoDxDetected,
            _ => ""
        };
    }

    private static void AddLegacyReFrameworkFinding(
        GameEntry game,
        ExistingFileSnapshot snapshot,
        ICollection<PrecheckFinding> findings,
        ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(game.ReframeworkUrl))
        {
            return;
        }

        var destinationName = Path.GetFileName(game.ReframeworkUrl.Trim());
        if (destinationName.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existingDinput = snapshot.Find("dinput8.dll");
        if (existingDinput is null)
        {
            return;
        }

        findings.Add(new PrecheckFinding
        {
            Type = PrecheckFindingType.LegacyReFramework,
            RelativePath = "dinput8.dll",
            OwnerKind = existingDinput.OwnerKind,
            Warning = PrecheckWarningCodes.ReFrameworkLegacy
        });
        warnings.Add(PrecheckWarningCodes.ReFrameworkLegacy);
    }
}

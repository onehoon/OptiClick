using OptiClick.Core.Install.Planning;

namespace OptiClick.Core.Install;

public sealed class CoreInstallConfigEditPolicy
{
    private sealed record ConfigEditPolicy(
        bool CreatesFileAllowed,
        bool CreatesMissingPathAllowed,
        bool AllowsAddMissingKey,
        bool AllowsAddMissingSection,
        bool UsesValuePathHint,
        string Notes);

    private static readonly IReadOnlyDictionary<CoreInstallPlanConfigEditType, ConfigEditPolicy> ConfigEditPolicies =
        new Dictionary<CoreInstallPlanConfigEditType, ConfigEditPolicy>
        {
            [CoreInstallPlanConfigEditType.GameIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: true,
                AllowsAddMissingKey: true,
                AllowsAddMissingSection: false,
                UsesValuePathHint: false,
                Notes: "Upsert missing keys only. Missing sections are not created."),
            [CoreInstallPlanConfigEditType.GameUnrealIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: false,
                AllowsAddMissingKey: false,
                AllowsAddMissingSection: false,
                UsesValuePathHint: true,
                Notes: "Missing value_path is skipped. No struct path creation."),
            [CoreInstallPlanConfigEditType.GameJsonProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: false,
                CreatesMissingPathAllowed: false,
                AllowsAddMissingKey: false,
                AllowsAddMissingSection: false,
                UsesValuePathHint: true,
                Notes: "JSON pointer must already exist. Missing paths are skipped."),
            [CoreInstallPlanConfigEditType.EngineIniProfile] = new ConfigEditPolicy(
                CreatesFileAllowed: true,
                CreatesMissingPathAllowed: true,
                AllowsAddMissingKey: true,
                AllowsAddMissingSection: true,
                UsesValuePathHint: false,
                Notes: "Engine.ini can be created and set read-only after apply."),
            [CoreInstallPlanConfigEditType.RegistryProfile] = new ConfigEditPolicy(
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

    public IReadOnlyList<CoreInstallPlanConfigEdit> ResolveConfigEdits(IReadOnlyList<CoreInstallConfigProfileHint> hints)
    {
        if (hints is null || hints.Count == 0)
        {
            return Array.Empty<CoreInstallPlanConfigEdit>();
        }

        var edits = new List<CoreInstallPlanConfigEdit>(hints.Count);
        foreach (var hint in hints)
        {
            edits.Add(CreateConfigEdit(hint));
        }

        return edits;
    }

    private static CoreInstallPlanConfigEdit CreateConfigEdit(CoreInstallConfigProfileHint hint)
    {
        var policy = ConfigEditPolicies.TryGetValue(hint.Type, out var configured)
            ? configured
            : DefaultConfigEditPolicy;

        return new CoreInstallPlanConfigEdit
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
}

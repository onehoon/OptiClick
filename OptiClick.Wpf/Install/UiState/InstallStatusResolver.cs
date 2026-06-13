using System.IO;
using OptiClick.Core.OptiScaler;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Precheck;

namespace OptiClick.Wpf.Install.UiState;

public sealed record InstallStatusSnapshot
{
    public string Code { get; init; } = InstallStatusCodes.Installable;
    public string Label { get; init; } = "Not Installed";
    public string BadgeCode { get; init; } = "";
    public string BadgeLabel { get; init; } = "";
    public string InstalledVersion { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string CurrentDisplayVersion { get; init; } = "";
    public string DetectedFile { get; init; } = "";
    public string Source { get; init; } = "";
}

public sealed record InstallStatusResolveInput
{
    public string TargetPath { get; init; } = "";
    public string CurrentVariant { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string CurrentDisplayVersion { get; init; } = "";
    public string CurrentFileVersion { get; init; } = "";
    public string CurrentProductVersion { get; init; } = "";
    public OptiScalerVersionIdentity StableTarget { get; init; } = new();
    public OptiScalerVersionIdentity PreviewTarget { get; init; } = new();
    public string Language { get; init; } = "en";
}

public interface IInstallStatusResolver
{
    InstallStatusSnapshot Resolve(InstallStatusResolveInput input);
}

public sealed class InstallStatusResolver : IInstallStatusResolver
{
    private static readonly string[] ManagedCandidates =
    [
        "OptiScaler.asi",
        "OptiScaler.dll",
        "dxgi.dll",
        "winmm.dll",
        "d3d12.dll",
        "dbghelp.dll",
        "version.dll",
        "wininet.dll",
        "winhttp.dll"
    ];

    private const string OptiScalerOriginalFileName = "optiscaler.dll";

    private readonly IInstallFileSystem _fileSystem;
    private readonly IFileVersionInfoReader _versionInfoReader;

    public InstallStatusResolver(IInstallFileSystem fileSystem, IFileVersionInfoReader versionInfoReader)
    {
        _fileSystem = fileSystem;
        _versionInfoReader = versionInfoReader;
    }

    public InstallStatusSnapshot Resolve(InstallStatusResolveInput input)
    {
        var targetPath = (input.TargetPath ?? "").Trim();
        var currentVersion = (input.CurrentVersion ?? "").Trim();
        var currentDisplayVersion = (input.CurrentDisplayVersion ?? "").Trim();
        var currentIdentity = new OptiScalerVersionIdentity
        {
            Variant = input.CurrentVariant,
            FileVersion = PickFirst(input.CurrentFileVersion, currentVersion),
            ProductVersion = input.CurrentProductVersion,
            DisplayVersion = currentDisplayVersion
        };
        var hasChannelTargets = OptiScalerVersionTargetPolicy.HasVersionIdentity(input.StableTarget)
                                || OptiScalerVersionTargetPolicy.HasVersionIdentity(input.PreviewTarget);
        var targetSet = new OptiScalerVersionTargetSet
        {
            Selected = currentIdentity,
            Stable = input.StableTarget,
            Preview = input.PreviewTarget
        };
        if (!_fileSystem.DirectoryExists(targetPath))
        {
            return BuildStatus(
                InstallStatusCodes.Installable,
                input.Language,
                "",
                currentVersion,
                currentDisplayVersion,
                badgeDecision: OptiScalerInstalledBadgePolicy.Evaluate(null, currentIdentity, null));
        }

        var detections = new List<(
            string CandidateName,
            OptiScalerVersionIdentity InstalledIdentity,
            OptiScalerVersionIdentity TargetIdentity,
            OptiScalerVersionUpdateDecision Decision)>();
        foreach (var candidate in ManagedCandidates)
        {
            var candidatePath = Path.Combine(targetPath, candidate);
            if (!_fileSystem.FileExists(candidatePath))
            {
                continue;
            }

            var versionInfo = _versionInfoReader.ReadVersionStrings(candidatePath);
            if (!IsOptiScalerBinary(versionInfo))
            {
                continue;
            }

            var installedIdentity = new OptiScalerVersionIdentity
            {
                FileVersion = ReadVersionString(versionInfo, "FileVersion"),
                ProductVersion = ReadVersionString(versionInfo, "ProductVersion")
            };
            var targetIdentity = hasChannelTargets
                ? OptiScalerVersionTargetPolicy.ResolveTargetForInstalled(installedIdentity, targetSet)
                : currentIdentity;
            detections.Add((
                candidate,
                installedIdentity,
                targetIdentity,
                OptiScalerVersionUpdatePolicy.Evaluate(installedIdentity, targetIdentity)));
        }

        if (detections.Count == 0)
        {
            return BuildStatus(
                InstallStatusCodes.Installable,
                input.Language,
                "",
                currentVersion,
                currentDisplayVersion,
                badgeDecision: OptiScalerInstalledBadgePolicy.Evaluate(null, currentIdentity, null));
        }

        foreach (var detection in detections)
        {
            if (detection.Decision.Code == OptiScalerVersionUpdateCode.UpdateAvailable)
            {
                return BuildStatus(
                    InstallStatusCodes.UpdateAvailable,
                    input.Language,
                    detection.Decision.InstalledDisplayVersion,
                    ResolveTargetVersion(detection.TargetIdentity),
                    detection.Decision.TargetDisplayVersion,
                    detection.CandidateName,
                    "binary",
                    OptiScalerInstalledBadgePolicy.Evaluate(
                        detection.InstalledIdentity,
                        detection.TargetIdentity,
                        detection.Decision));
            }
        }

        var first = detections[0];
        if (first.Decision.Code == OptiScalerVersionUpdateCode.PreRelease)
        {
            return BuildStatus(
                InstallStatusCodes.PreRelease,
                input.Language,
                first.Decision.InstalledDisplayVersion,
                ResolveTargetVersion(first.TargetIdentity),
                first.Decision.TargetDisplayVersion,
                first.CandidateName,
                "binary",
                OptiScalerInstalledBadgePolicy.Evaluate(
                    first.InstalledIdentity,
                    first.TargetIdentity,
                    first.Decision));
        }

        return BuildStatus(
            InstallStatusCodes.Latest,
            input.Language,
            first.Decision.InstalledDisplayVersion,
            ResolveTargetVersion(first.TargetIdentity),
            first.Decision.TargetDisplayVersion,
            first.CandidateName,
            "binary",
            OptiScalerInstalledBadgePolicy.Evaluate(
                first.InstalledIdentity,
                first.TargetIdentity,
                first.Decision));
    }

    private static bool IsOptiScalerBinary(IReadOnlyDictionary<string, string> versionInfo)
    {
        var originalFilename = "";
        if (versionInfo.TryGetValue("OriginalFilename", out var value))
        {
            originalFilename = (value ?? "").Trim().ToLowerInvariant();
        }

        return string.Equals(originalFilename, OptiScalerOriginalFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadVersionString(IReadOnlyDictionary<string, string> versionInfo, string key)
    {
        if (!versionInfo.TryGetValue(key, out var value))
        {
            return "";
        }

        return (value ?? "").Trim();
    }

    private static string PickFirst(params string[] values)
    {
        foreach (var value in values)
        {
            var normalized = (value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return "";
    }

    private static string ResolveTargetVersion(OptiScalerVersionIdentity target)
    {
        return PickFirst(target.FileVersion, target.ProductVersion, target.DisplayVersion);
    }

    private static InstallStatusSnapshot BuildStatus(
        string code,
        string language,
        string installedVersion,
        string currentVersion,
        string currentDisplayVersion,
        string detectedFile = "",
        string source = "",
        OptiScalerInstalledBadgeDecision? badgeDecision = null)
    {
        var safeBadgeDecision = badgeDecision
                                ?? OptiScalerInstalledBadgePolicy.Evaluate(null, null, null);
        return new InstallStatusSnapshot
        {
            Code = code,
            Label = ResolveStatusLabel(code, language, installedVersion),
            BadgeCode = ResolveBadgeCode(safeBadgeDecision),
            BadgeLabel = ResolveBadgeLabel(safeBadgeDecision, language),
            InstalledVersion = installedVersion,
            CurrentVersion = currentVersion,
            CurrentDisplayVersion = currentDisplayVersion,
            DetectedFile = detectedFile,
            Source = source
        };
    }

    private static string ResolveBadgeCode(OptiScalerInstalledBadgeDecision decision)
    {
        return decision.Code switch
        {
            OptiScalerInstalledBadgeCode.UpdateAvailable => InstallStatusBadgeCodes.UpdateAvailable,
            OptiScalerInstalledBadgeCode.LatestStable => InstallStatusBadgeCodes.Latest,
            OptiScalerInstalledBadgeCode.PreviewInstalled => InstallStatusBadgeCodes.PreviewInstalled,
            OptiScalerInstalledBadgeCode.InstalledVersion => InstallStatusBadgeCodes.InstalledVersion,
            _ => InstallStatusBadgeCodes.Installable
        };
    }

    private static string ResolveBadgeLabel(OptiScalerInstalledBadgeDecision decision, string language)
    {
        var isKorean = IsKoreanLanguage(language);
        var displayVersion = (decision.DisplayVersion ?? "").Trim();
        return decision.Code switch
        {
            OptiScalerInstalledBadgeCode.UpdateAvailable => string.IsNullOrWhiteSpace(displayVersion)
                ? (isKorean ? "\uC5C5\uB370\uC774\uD2B8" : "Update")
                : string.Format(
                    isKorean ? "\uC5C5\uB370\uC774\uD2B8 ({0})" : "Update ({0})",
                    displayVersion),
            OptiScalerInstalledBadgeCode.LatestStable => isKorean ? "\uCD5C\uC2E0" : "Latest",
            OptiScalerInstalledBadgeCode.PreviewInstalled => displayVersion,
            OptiScalerInstalledBadgeCode.InstalledVersion => displayVersion,
            _ => isKorean ? "\uBBF8\uC124\uCE58" : "Not Installed"
        };
    }

    private static string ResolveStatusLabel(string code, string language, string installedVersion)
    {
        var isKorean = IsKoreanLanguage(language);
        return code switch
        {
            InstallStatusCodes.UpdateAvailable => isKorean ? "업데이트" : "Update",
            InstallStatusCodes.Latest => isKorean ? "최신" : "Latest",
            InstallStatusCodes.PreRelease => string.IsNullOrWhiteSpace(installedVersion)
                ? "Pre"
                : $"Pre ({installedVersion})",
            InstallStatusCodes.NeedsReview => isKorean ? "확인" : "Check",
            _ => isKorean ? "미설치" : "Not Installed"
        };
    }

    private static bool IsKoreanLanguage(string language)
    {
        return (language ?? "").Trim().StartsWith("ko", StringComparison.OrdinalIgnoreCase);
    }
}

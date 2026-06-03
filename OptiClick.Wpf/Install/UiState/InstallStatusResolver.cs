using System.IO;
using System.Text.RegularExpressions;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Install.Precheck;

namespace OptiClick.Wpf.Install.UiState;

public sealed record InstallStatusSnapshot
{
    public string Code { get; init; } = InstallStatusCodes.Installable;
    public string Label { get; init; } = "Not Installed";
    public string InstalledVersion { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string CurrentDisplayVersion { get; init; } = "";
    public string DetectedFile { get; init; } = "";
    public string Source { get; init; } = "";
}

public sealed record InstallStatusResolveInput
{
    public string TargetPath { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string CurrentDisplayVersion { get; init; } = "";
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
    private static readonly Regex VersionTokenRegex = new(@"\d+(?:[\.,]\d+)*", RegexOptions.Compiled);

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
        if (!_fileSystem.DirectoryExists(targetPath))
        {
            return BuildStatus(InstallStatusCodes.Installable, input.Language, "", currentVersion, currentDisplayVersion);
        }

        var detections = new List<(string CandidateName, string InstalledVersion, int? Comparison)>();
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

            var installedVersion = ExtractComparableBinaryVersion(versionInfo);
            var comparison = CompareVersions(installedVersion, currentVersion);
            detections.Add((candidate, installedVersion, comparison));
        }

        if (detections.Count == 0)
        {
            return BuildStatus(InstallStatusCodes.Installable, input.Language, "", currentVersion, currentDisplayVersion);
        }

        foreach (var detection in detections)
        {
            if (detection.Comparison is null || detection.Comparison < 0)
            {
                return BuildStatus(
                    InstallStatusCodes.UpdateAvailable,
                    input.Language,
                    detection.InstalledVersion,
                    currentVersion,
                    currentDisplayVersion,
                    detection.CandidateName,
                    "binary");
            }
        }

        var first = detections[0];
        if (first.Comparison is not null && first.Comparison > 0)
        {
            return BuildStatus(
                InstallStatusCodes.PreRelease,
                input.Language,
                first.InstalledVersion,
                currentVersion,
                currentDisplayVersion,
                first.CandidateName,
                "binary");
        }

        return BuildStatus(
            InstallStatusCodes.Latest,
            input.Language,
            first.InstalledVersion,
            currentVersion,
            currentDisplayVersion,
            first.CandidateName,
            "binary");
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

    private static string ExtractComparableBinaryVersion(IReadOnlyDictionary<string, string> versionInfo)
    {
        foreach (var key in new[] { "FileVersion", "ProductVersion" })
        {
            if (!versionInfo.TryGetValue(key, out var value))
            {
                continue;
            }

            var text = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (ParseVersionTuple(text).Length > 0)
            {
                return text;
            }
        }

        return "";
    }

    private static int? CompareVersions(string left, string right)
    {
        var leftParts = ParseVersionTuple(left);
        var rightParts = ParseVersionTuple(right);
        if (leftParts.Length == 0 || rightParts.Length == 0)
        {
            return null;
        }

        var size = Math.Max(leftParts.Length, rightParts.Length);
        for (var i = 0; i < size; i++)
        {
            var leftValue = i < leftParts.Length ? leftParts[i] : 0;
            var rightValue = i < rightParts.Length ? rightParts[i] : 0;
            if (leftValue < rightValue) return -1;
            if (leftValue > rightValue) return 1;
        }

        return 0;
    }

    private static int[] ParseVersionTuple(string value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<int>();
        }

        var match = VersionTokenRegex.Match(text);
        if (!match.Success)
        {
            return Array.Empty<int>();
        }

        var tokens = match.Value.Split(new[] { '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var parsed = new List<int>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!int.TryParse(token, out var number))
            {
                return Array.Empty<int>();
            }

            parsed.Add(number);
        }

        return parsed.ToArray();
    }

    private static InstallStatusSnapshot BuildStatus(
        string code,
        string language,
        string installedVersion,
        string currentVersion,
        string currentDisplayVersion,
        string detectedFile = "",
        string source = "")
    {
        return new InstallStatusSnapshot
        {
            Code = code,
            Label = ResolveStatusLabel(code, language, installedVersion),
            InstalledVersion = installedVersion,
            CurrentVersion = currentVersion,
            CurrentDisplayVersion = currentDisplayVersion,
            DetectedFile = detectedFile,
            Source = source
        };
    }

    private static string ResolveStatusLabel(string code, string language, string installedVersion)
    {
        var isKorean = (language ?? "").Trim().StartsWith("ko", StringComparison.OrdinalIgnoreCase);
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
}

using System.Text.Json;
using OptiClick.Core.RuntimeData;

namespace OptiClick.Core.Games.GpuBundle;

public sealed class RemoteGpuBundle
{
    public IReadOnlyDictionary<string, RemoteGpuBundleGameEntry> GamesByGameId { get; init; } =
        new Dictionary<string, RemoteGpuBundleGameEntry>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RuntimeDataRawRow> SharedOptiScalerIniRows { get; init; } = [];
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;
    public IReadOnlyDictionary<string, JsonElement> RawValues { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}

public sealed class RemoteGpuBundleGameEntry
{
    public string GameId { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public string BundleGpuVendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string BundleGpuGroup { get; init; } = "";
    public string BundleManifestVersion { get; init; } = "";
    public Fsr4ManifestPolicy Fsr4 { get; init; } = Fsr4ManifestPolicy.Disabled;

    public RemoteGpuBundleInstallProfile InstallProfile { get; init; } = new();
    public IReadOnlyList<RuntimeDataRawRow> LocalOptiScalerIniRows { get; init; } = [];
    public IReadOnlyDictionary<string, JsonElement> RawValues { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}

public sealed class RemoteGpuBundleInstallProfile
{
    public bool Enabled { get; init; } = true;

    public string OptiScalerDllName { get; init; } = "";
    public string ReFrameworkUrl { get; init; } = "";
    public string SpecialK { get; init; } = "";
    public string ExtraBundle { get; init; } = "";
    public string ExcludeListRaw { get; init; } = "";
    public IReadOnlyList<string> ExcludeListPatterns { get; init; } = [];

    public bool UltimateAsiLoader { get; init; }
    public bool OptiPatcher { get; init; }
    public bool Unreal5 { get; init; }
    public bool RtssOverlay { get; init; }

    public IReadOnlyDictionary<string, JsonElement> RawValues { get; init; } =
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
}

public sealed class RemoteGpuBundleParseResult
{
    public static readonly RemoteGpuBundleParseResult Empty = new();

    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public RemoteGpuBundle Bundle { get; init; } = new();

    public static RemoteGpuBundleParseResult Success(RemoteGpuBundle bundle)
    {
        return new RemoteGpuBundleParseResult
        {
            IsSuccess = true,
            Bundle = bundle ?? new RemoteGpuBundle()
        };
    }

    public static RemoteGpuBundleParseResult Failure(string errorCode, string errorMessage = "")
    {
        return new RemoteGpuBundleParseResult
        {
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim(),
            ErrorMessage = (errorMessage ?? "").Trim(),
            Bundle = new RemoteGpuBundle()
        };
    }
}

public interface IRemoteGpuBundleParser
{
    RemoteGpuBundleParseResult Parse(
        string json,
        string? selectedGpuGroup = null,
        string? requestVendor = null,
        string? bundleKey = null,
        string? manifestVersion = null,
        Fsr4ManifestPolicy? fsr4Policy = null);
}

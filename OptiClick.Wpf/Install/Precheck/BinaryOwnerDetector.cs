using System.IO;
using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Precheck;

public interface IFileVersionInfoReader
{
    IReadOnlyDictionary<string, string> ReadVersionStrings(string filePath);
}

public sealed class WindowsFileVersionInfoReader : IFileVersionInfoReader
{
    private readonly OptiClick.Infrastructure.FileSystem.FileVersionInfoReader _inner;

    public WindowsFileVersionInfoReader()
        : this(new OptiClick.Infrastructure.FileSystem.FileVersionInfoReader())
    {
    }

    internal WindowsFileVersionInfoReader(OptiClick.Infrastructure.FileSystem.FileVersionInfoReader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyDictionary<string, string> ReadVersionStrings(string filePath) =>
        _inner.ReadVersionStrings(filePath);
}

public interface IBinaryOwnerDetector
{
    string DetectOwner(string filePath);
}

public sealed class BinaryOwnerDetector : IBinaryOwnerDetector
{
    private static readonly IReadOnlyDictionary<string, string[]> OwnerKeywords = new Dictionary<string, string[]>
    {
        [ModConflictKinds.ReShade] = ["reshade"],
        [ModConflictKinds.SpecialK] = ["special k", "specialk"]
    };

    private readonly IFileSignatureDetectors _signatureDetectors;
    private readonly IFileVersionInfoReader _versionInfoReader;

    public BinaryOwnerDetector(IFileSignatureDetectors signatureDetectors, IFileVersionInfoReader versionInfoReader)
    {
        _signatureDetectors = signatureDetectors;
        _versionInfoReader = versionInfoReader;
    }

    public string DetectOwner(string filePath)
    {
        if (_signatureDetectors.IsOptiScalerManagedProxyDll(filePath))
        {
            return "optiscaler";
        }

        var versionInfo = _versionInfoReader.ReadVersionStrings(filePath);
        if (HasReShadeVersionAttribute(versionInfo))
        {
            return ModConflictKinds.ReShade;
        }

        var haystack = string.Join(
            " ",
            new[] { Path.GetFileName(filePath) }
                .Concat(versionInfo.Values)
                .Where(value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();

        foreach (var (owner, keywords) in OwnerKeywords)
        {
            if (owner == ModConflictKinds.ReShade)
            {
                // ReShade is recognized only by metadata.
                continue;
            }

            if (keywords.Any(keyword => haystack.Contains(keyword, StringComparison.Ordinal)))
            {
                return owner;
            }
        }

        return "";
    }

    private static bool HasReShadeVersionAttribute(IReadOnlyDictionary<string, string> versionInfo)
    {
        foreach (var value in versionInfo.Values)
        {
            if ((value ?? "").Contains("reshade", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

using System.Diagnostics;
using System.IO;

namespace OptiClick.Infrastructure.FileSystem;

public sealed class FileSignatureDetectors
{
    private readonly Func<string, bool> _fileExists;

    public FileSignatureDetectors()
        : this(static path => File.Exists(path))
    {
    }

    public FileSignatureDetectors(Func<string, bool> fileExists)
    {
        _fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
    }

    public bool IsOptiScalerManagedProxyDll(string filePath)
    {
        var version = ReadVersion(filePath);
        var originalFileName = GetVersionValue(version, "OriginalFilename");
        return string.Equals(originalFileName, "OptiScaler.dll", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsReShadeDll(string filePath)
    {
        var text = string.Join(' ', ReadVersion(filePath).Values).ToLowerInvariant();
        return text.Contains("reshade", StringComparison.Ordinal);
    }

    public bool IsSpecialKDll(string filePath)
    {
        var text = string.Join(' ', ReadVersion(filePath).Values).ToLowerInvariant();
        return text.Contains("special k", StringComparison.Ordinal) || text.Contains("specialk", StringComparison.Ordinal);
    }

    private Dictionary<string, string> ReadVersion(string filePath)
    {
        if (!_fileExists(filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var info = FileVersionInfo.GetVersionInfo(filePath);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["OriginalFilename"] = info.OriginalFilename ?? "",
                ["CompanyName"] = info.CompanyName ?? "",
                ["FileDescription"] = info.FileDescription ?? "",
                ["ProductName"] = info.ProductName ?? "",
                ["InternalName"] = info.InternalName ?? "",
                ["FileVersion"] = info.FileVersion ?? "",
                ["ProductVersion"] = info.ProductVersion ?? ""
            };
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string GetVersionValue(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : "";
    }
}

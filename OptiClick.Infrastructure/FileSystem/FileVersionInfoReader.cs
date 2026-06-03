using System.Diagnostics;
using System.IO;

namespace OptiClick.Infrastructure.FileSystem;

public sealed class FileVersionInfoReader
{
    public IReadOnlyDictionary<string, string> ReadVersionStrings(string filePath)
    {
        if (!File.Exists(filePath))
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
}

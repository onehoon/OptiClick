using System.IO;
using OptiClick.Core.Scan;

namespace OptiClick.Infrastructure.Scan;

public sealed class ScanFileSystemProbe : IScanFileSystemProbe
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }
}

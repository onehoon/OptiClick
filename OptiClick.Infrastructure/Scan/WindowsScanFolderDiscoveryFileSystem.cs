using System.IO;

namespace OptiClick.Infrastructure.Scan;

public interface IScanFolderDiscoveryFileSystem
{
    IReadOnlyList<string> GetFixedReadyDriveRoots();
    bool DirectoryExists(string path);
    bool FileExists(string path);
    string[] ReadAllLines(string path);
}

public sealed class WindowsScanFolderDiscoveryFileSystem : IScanFolderDiscoveryFileSystem
{
    public IReadOnlyList<string> GetFixedReadyDriveRoots()
    {
        var roots = new List<string>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                var root = (drive.RootDirectory?.FullName ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(root))
                {
                    roots.Add(root);
                }
            }
            catch
            {
                // Skip inaccessible drive and continue discovery.
            }
        }

        return roots;
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public string[] ReadAllLines(string path)
    {
        return File.ReadAllLines(path);
    }
}

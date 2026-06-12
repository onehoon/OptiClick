namespace OptiClick.Core.Scan;

public interface IScanFileSystemProbe
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
}

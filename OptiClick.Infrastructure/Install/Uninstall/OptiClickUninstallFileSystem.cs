using System.IO;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Infrastructure.Install.Uninstall;

public sealed class OptiClickUninstallFileSystem : IOptiClickUninstallFileSystem
{
    private readonly InstallFileSystem _inner;

    public OptiClickUninstallFileSystem()
        : this(new InstallFileSystem())
    {
    }

    internal OptiClickUninstallFileSystem(InstallFileSystem inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool FileExists(string path) => _inner.FileExists(path);

    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    public bool IsWritable(string path) => _inner.IsWritable(path);

    public void SetWritable(string path) => _inner.SetWritable(path);

    public void DeleteFile(string path) => _inner.DeleteFile(path);

    public void DeleteDirectory(string path, bool recursive = true) => _inner.DeleteDirectory(path, recursive);

    public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        => _inner.EnumerateFiles(directoryPath, searchPattern, searchOption);

    public IEnumerable<string> EnumerateDirectories(string directoryPath, string searchPattern, SearchOption searchOption)
        => _inner.EnumerateDirectories(directoryPath, searchPattern, searchOption);
}

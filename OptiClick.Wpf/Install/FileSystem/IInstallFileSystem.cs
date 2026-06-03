using System.IO;

namespace OptiClick.Wpf.Install.FileSystem;

public interface IInstallFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive = true);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
    void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite);
    void MoveDirectory(string sourceDirectory, string destinationDirectory);
    void MoveFile(string sourcePath, string destinationPath, bool overwrite);
    void SetWritable(string path);
    bool IsWritable(string path);
    IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption);
    IEnumerable<string> EnumerateFileSystemEntries(string directoryPath);
}

public sealed class InstallFileSystem : IInstallFileSystem
{
    private readonly OptiClick.Infrastructure.FileSystem.InstallFileSystem _inner;

    public InstallFileSystem()
        : this(new OptiClick.Infrastructure.FileSystem.InstallFileSystem())
    {
    }

    internal InstallFileSystem(OptiClick.Infrastructure.FileSystem.InstallFileSystem inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool FileExists(string path) => _inner.FileExists(path);
    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
    public void CreateDirectory(string path) => _inner.CreateDirectory(path);
    public void DeleteFile(string path) => _inner.DeleteFile(path);
    public void DeleteDirectory(string path, bool recursive = true) => _inner.DeleteDirectory(path, recursive);
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => _inner.CopyFile(sourcePath, destinationPath, overwrite);
    public void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite) => _inner.CopyDirectory(sourceDirectory, destinationDirectory, overwrite);
    public void MoveDirectory(string sourceDirectory, string destinationDirectory) => _inner.MoveDirectory(sourceDirectory, destinationDirectory);
    public void MoveFile(string sourcePath, string destinationPath, bool overwrite) => _inner.MoveFile(sourcePath, destinationPath, overwrite);
    public void SetWritable(string path) => _inner.SetWritable(path);
    public bool IsWritable(string path) => _inner.IsWritable(path);
    public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption) => _inner.EnumerateFiles(directoryPath, searchPattern, searchOption);
    public IEnumerable<string> EnumerateFileSystemEntries(string directoryPath) => _inner.EnumerateFileSystemEntries(directoryPath);
}

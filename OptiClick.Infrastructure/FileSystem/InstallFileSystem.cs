using System.IO;

namespace OptiClick.Infrastructure.FileSystem;

public sealed class InstallFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void DeleteFile(string path) => File.Delete(path);
    public void DeleteDirectory(string path, bool recursive = true) => Directory.Delete(path, recursive);
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => File.Copy(sourcePath, destinationPath, overwrite);

    public void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
    {
        var source = new DirectoryInfo(sourceDirectory);
        if (!source.Exists)
        {
            throw new DirectoryNotFoundException(sourceDirectory);
        }

        Directory.CreateDirectory(destinationDirectory);
        foreach (var file in source.GetFiles("*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source.FullName, file.FullName);
            var destinationFile = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(file.FullName, destinationFile, overwrite);
        }
    }

    public void MoveDirectory(string sourceDirectory, string destinationDirectory) => Directory.Move(sourceDirectory, destinationDirectory);

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (overwrite && File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(sourcePath, destinationPath);
    }

    public void SetWritable(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }

    public bool IsWritable(string path)
    {
        var attributes = File.GetAttributes(path);
        return (attributes & FileAttributes.ReadOnly) == 0;
    }

    public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateFiles(directoryPath, searchPattern, searchOption);

    public IEnumerable<string> EnumerateDirectories(string directoryPath, string searchPattern, SearchOption searchOption)
        => Directory.EnumerateDirectories(directoryPath, searchPattern, searchOption);

    public IEnumerable<string> EnumerateFileSystemEntries(string directoryPath)
        => Directory.EnumerateFileSystemEntries(directoryPath);
}

using System.IO;
using OptiClick.Infrastructure.FileSystem;

namespace OptiClick.Wpf.Install.Archives;

public sealed class TemporaryExtractionScope : IDisposable
{
    private readonly bool _ownsPath;
    private bool _disposed;

    public TemporaryExtractionScope(string path, bool ownsPath = true)
    {
        Path = System.IO.Path.GetFullPath(path ?? "");
        _ownsPath = ownsPath;
    }

    public string Path { get; }

    public static TemporaryExtractionScope Create(string prefix)
    {
        var pathProvider = new AppLocalDataPathProvider();
        var root = System.IO.Path.Combine(
            pathProvider.InstallExecutionTempDirectory,
            $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return new TemporaryExtractionScope(root, ownsPath: true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_ownsPath)
        {
            return;
        }

        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }
}

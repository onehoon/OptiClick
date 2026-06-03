using System.IO;

namespace OptiClick.Wpf.Install.Gates;

public sealed class WritePermissionProbe : IWritePermissionProbe
{
    private readonly IWritePermissionProbeFileSystem _fileSystem;

    public WritePermissionProbe()
        : this(new WritePermissionProbeFileSystem())
    {
    }

    internal WritePermissionProbe(IWritePermissionProbeFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public WritePermissionProbeResult Probe(string targetFolder)
    {
        var normalizedTargetFolder = (targetFolder ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedTargetFolder))
        {
            return Failure("invalid_target_folder");
        }

        if (_fileSystem.FileExists(normalizedTargetFolder))
        {
            return Failure("invalid_target_folder");
        }

        if (!_fileSystem.DirectoryExists(normalizedTargetFolder))
        {
            return Failure("invalid_target_folder");
        }

        var token = Guid.NewGuid().ToString("N");
        var probePath = Path.Combine(normalizedTargetFolder, $".optiscaler_write_test_{token}.tmp");
        var renamedProbePath = Path.Combine(normalizedTargetFolder, $".optiscaler_write_test_{token}.renamed.tmp");

        try
        {
            if (!_fileSystem.TryCreateProbeFile(probePath))
            {
                return Failure("probe_create_failed");
            }

            if (!_fileSystem.TryMove(probePath, renamedProbePath))
            {
                TryCleanup(probePath, renamedProbePath);
                return Failure("probe_rename_failed");
            }

            if (!_fileSystem.TryDelete(renamedProbePath))
            {
                TryCleanup(probePath, renamedProbePath);
                return Failure("probe_delete_failed");
            }

            return Success();
        }
        catch
        {
            TryCleanup(probePath, renamedProbePath);
            return Failure("probe_create_failed");
        }
    }

    private void TryCleanup(string probePath, string renamedProbePath)
    {
        foreach (var path in new[] { probePath, renamedProbePath })
        {
            try
            {
                if (_fileSystem.FileExists(path))
                {
                    _fileSystem.TryDelete(path);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }

    private static WritePermissionProbeResult Success() => new()
    {
        IsSuccess = true
    };

    private static WritePermissionProbeResult Failure(string errorCode) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode
    };
}

internal interface IWritePermissionProbeFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    bool TryCreateProbeFile(string path);
    bool TryMove(string source, string destination);
    bool TryDelete(string path);
}

internal sealed class WritePermissionProbeFileSystem : IWritePermissionProbeFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);

    public bool TryCreateProbeFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write("write-test");
            writer.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryMove(string source, string destination)
    {
        try
        {
            File.Move(source, destination);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}


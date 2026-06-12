using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Gates;

public interface IInstallTargetPathValidator
{
    InstallTargetPathValidationResult Validate(string? candidatePath);
}

public sealed record InstallTargetPathValidationResult
{
    public string NormalizedTargetDirectory { get; init; } = "";
    public bool IsValidTargetDirectory { get; init; }
}

public interface ILocalFileSystemPathProbe
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
}

public sealed class LocalFileSystemPathProbe : ILocalFileSystemPathProbe
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
}

public sealed class InstallTargetPathValidator : IInstallTargetPathValidator
{
    private readonly ILocalFileSystemPathProbe _pathProbe;

    public InstallTargetPathValidator()
        : this(new LocalFileSystemPathProbe())
    {
    }

    internal InstallTargetPathValidator(ILocalFileSystemPathProbe pathProbe)
    {
        _pathProbe = pathProbe ?? throw new ArgumentNullException(nameof(pathProbe));
    }

    public InstallTargetPathValidationResult Validate(string? candidatePath)
    {
        var normalizedTargetDirectory = NormalizeTargetDirectory(candidatePath);
        var isValid = !string.IsNullOrWhiteSpace(normalizedTargetDirectory)
                      && !_pathProbe.FileExists(normalizedTargetDirectory)
                      && _pathProbe.DirectoryExists(normalizedTargetDirectory);

        return new InstallTargetPathValidationResult
        {
            NormalizedTargetDirectory = normalizedTargetDirectory,
            IsValidTargetDirectory = isValid
        };
    }

    public string NormalizeTargetDirectory(string? candidatePath)
    {
        var normalized = (candidatePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (_pathProbe.DirectoryExists(normalized))
        {
            return InstallTargetPathPolicy.NormalizeTargetDirectory(normalized);
        }

        if (_pathProbe.FileExists(normalized))
        {
            return InstallTargetPathPolicy.NormalizeTargetDirectory(Path.GetDirectoryName(normalized));
        }

        try
        {
            var fullPath = Path.GetFullPath(normalized);
            if (_pathProbe.DirectoryExists(fullPath))
            {
                return InstallTargetPathPolicy.NormalizeTargetDirectory(fullPath);
            }

            if (_pathProbe.FileExists(fullPath))
            {
                return InstallTargetPathPolicy.NormalizeTargetDirectory(Path.GetDirectoryName(fullPath));
            }

            return InstallTargetPathPolicy.NormalizeTargetDirectory(fullPath);
        }
        catch
        {
            return InstallTargetPathPolicy.NormalizeTargetDirectory(normalized);
        }
    }
}

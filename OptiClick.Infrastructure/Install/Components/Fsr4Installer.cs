using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IFsr4Installer
{
    Task<ComponentInstallStepResult> InstallAsync(Fsr4InstallContext context, CancellationToken cancellationToken = default);
}

public sealed class Fsr4Installer : IFsr4Installer
{
    private readonly IComponentArchiveExtractor _archiveExtractor;
    private readonly IComponentInstallFileSystem _fileSystem;
    private readonly IFsr4InstallEligibilityResolver _eligibilityResolver;
    private readonly string _installExecutionTempRoot;

    public Fsr4Installer(
        IComponentArchiveExtractor archiveExtractor,
        IComponentInstallFileSystem fileSystem,
        IFsr4InstallEligibilityResolver? eligibilityResolver = null,
        string installExecutionTempRoot = "")
    {
        _archiveExtractor = archiveExtractor ?? throw new ArgumentNullException(nameof(archiveExtractor));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _eligibilityResolver = eligibilityResolver ?? new Fsr4InstallEligibilityResolver();
        _installExecutionTempRoot = string.IsNullOrWhiteSpace(installExecutionTempRoot)
            ? Path.GetTempPath()
            : installExecutionTempRoot;
    }

    public async Task<ComponentInstallStepResult> InstallAsync(Fsr4InstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eligibility = _eligibilityResolver.Resolve(new Fsr4InstallEligibilityContext
        {
            UseFsr4 = context.UseFsr4,
            GpuVendor = context.GpuVendor,
            GpuName = context.GpuName,
            GpuBundleKey = context.GpuBundleKey,
            GpuGroup = context.GpuGroup
        });

        if (!eligibility.CanInstall)
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.Fsr4, eligibility.SkipReason);
        }

        var sourcePath = (context.Fsr4SourceArchivePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(sourcePath)
            || (!_fileSystem.FileExists(sourcePath) && !_fileSystem.DirectoryExists(sourcePath)))
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.Fsr4, ComponentInstallErrorCodes.SourceMissing);
        }

        var tempRoot = "";
        var payloadRoot = sourcePath;

        try
        {
            if (_fileSystem.FileExists(sourcePath))
            {
                tempRoot = Path.Combine(
                    _installExecutionTempRoot,
                    "Fsr4",
                    Guid.NewGuid().ToString("N"));
                payloadRoot = tempRoot;
                _fileSystem.CreateDirectory(tempRoot);
                var extract = await _archiveExtractor.ExtractAsync(sourcePath, tempRoot, cancellationToken);
                if (!extract.IsSuccess)
                {
                    return ComponentInstallStepResult.Failed(ComponentInstallName.Fsr4, ComponentInstallErrorCodes.ExtractFailed);
                }
            }

            var dllCandidates = FindDllCandidates(payloadRoot);
            if (dllCandidates.Count == 0)
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.Fsr4, ComponentInstallErrorCodes.SourceMissing);
            }

            if (dllCandidates.Count > 1)
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.Fsr4, ComponentInstallErrorCodes.MultipleCandidates);
            }

            var sourceDll = dllCandidates[0];
            var destination = Path.Combine(context.TargetPath, Path.GetFileName(sourceDll));
            if (_fileSystem.FileExists(destination))
            {
                InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, destination);
            }

            // FSR4 is an archive component; overwriting the resolved payload target is intentional.
            _fileSystem.CopyFile(sourceDll, destination, overwrite: true);
            return ComponentInstallStepResult.Success(ComponentInstallName.Fsr4);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempRoot))
            {
                TryDeleteDirectory(tempRoot);
            }
        }
    }

    private List<string> FindDllCandidates(string rootPath)
    {
        var candidates = new List<string>();
        var pending = new Queue<string>();
        pending.Enqueue(rootPath);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var entry in _fileSystem.EnumerateFileSystemEntries(current))
            {
                if (_fileSystem.DirectoryExists(entry))
                {
                    pending.Enqueue(entry);
                    continue;
                }

                if (_fileSystem.FileExists(entry)
                    && string.Equals(Path.GetExtension(entry), ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(entry);
                }
            }
        }

        return candidates;
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (_fileSystem.DirectoryExists(path))
            {
                _fileSystem.DeleteDirectory(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure.
        }
    }
}

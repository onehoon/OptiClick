using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IReFrameworkInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ReFrameworkInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class ReFrameworkInstaller : IReFrameworkInstaller
{
    private const string SourceDllName = "dinput8.dll";
    private readonly IDllPayloadInstaller _dllPayloadInstaller;
    private readonly IInstallFileSystem _fileSystem;
    private readonly IFileSignatureDetectors _detectors;

    public ReFrameworkInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _dllPayloadInstaller = dllPayloadInstaller ?? throw new ArgumentNullException(nameof(dllPayloadInstaller));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ReFrameworkInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var destination = context.ReFrameworkDestination;
        if (string.IsNullOrWhiteSpace(destination))
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.ReFramework);
        }

        string normalizedDestination;
        try
        {
            normalizedDestination = InstallerExecutionHelpers.NormalizeRelativeDllPath(destination);
        }
        catch (InvalidOperationException)
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.ReFramework, ComponentInstallErrorCodes.InvalidDestination);
        }

        CleanupLegacyFiles(context.TargetPath, normalizedDestination);

        var url = InstallerExecutionHelpers.ExtractModuleUrl(context.ModuleDownloadLinks, "reframework");
        var downloadFileName = ReadEntryFileName(context.ModuleDownloadLinks, "reframework");
        var sha256 = InstallerExecutionHelpers.ExtractModuleSha256(context.ModuleDownloadLinks, "reframework");
        var result = await _dllPayloadInstaller.InstallAsync(
            new DllPayloadInstallRequest
            {
                TargetPath = context.TargetPath,
                DestinationRelativePath = normalizedDestination,
                SourceDllName = SourceDllName,
                Url = url,
                CachedArchivePath = context.ReFrameworkCachedArchivePath,
                DownloadFileName = downloadFileName,
                Sha256 = sha256
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.ReFramework, result.ErrorCode);
        }

        return ComponentInstallStepResult.Success(ComponentInstallName.ReFramework);
    }

    private void CleanupLegacyFiles(string targetPath, string currentDestination)
    {
        var destinationFile = Path.GetFileName(currentDestination);
        if (string.Equals(destinationFile, "ReShade64.dll", StringComparison.OrdinalIgnoreCase))
        {
            DeleteIfRemovable(targetPath, "dinput8.dll");
            return;
        }

        if (string.Equals(destinationFile, "dinput8.dll", StringComparison.OrdinalIgnoreCase))
        {
            var reshadePath = Path.Combine(targetPath, "ReShade64.dll");
            if (!_fileSystem.FileExists(reshadePath))
            {
                return;
            }

            if (_detectors.IsReShadeDll(reshadePath))
            {
                return;
            }

            DeleteIfRemovable(targetPath, "ReShade64.dll");
        }
    }

    private void DeleteIfRemovable(string targetPath, string fileName)
    {
        var path = Path.Combine(targetPath, fileName);
        if (!_fileSystem.FileExists(path))
        {
            return;
        }

        InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, path);
        _fileSystem.DeleteFile(path);
    }

    private static string ReadEntryFileName(IReadOnlyDictionary<string, object?> links, string key)
    {
        if (!links.TryGetValue(key, out var rawEntry)
            || rawEntry is not IReadOnlyDictionary<string, object?> entry)
        {
            return "";
        }

        return InstallerExecutionHelpers.ReadString(entry, "filename");
    }
}

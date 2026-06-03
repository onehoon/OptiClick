using System.IO;

namespace OptiClick.Infrastructure.Install.Components;

public interface IUltimateAsiLoaderInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(UltimateAsiLoaderInstallContext context, CancellationToken cancellationToken = default);
}

public sealed class UltimateAsiLoaderInstaller : IUltimateAsiLoaderInstaller
{
    private const string PayloadDllName = "dinput8.dll";
    private readonly IDllPayloadInstaller _dllPayloadInstaller;
    private readonly IInstallFileSystem _fileSystem;
    private readonly IFileSignatureDetectors _detectors;

    public UltimateAsiLoaderInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _dllPayloadInstaller = dllPayloadInstaller ?? throw new ArgumentNullException(nameof(dllPayloadInstaller));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(UltimateAsiLoaderInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.UseUltimateAsiLoader)
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.UltimateAsiLoader);
        }

        var url = InstallerExecutionHelpers.ExtractModuleUrl(context.ModuleDownloadLinks, "ultimateasiloader");
        var downloadName = ReadEntryFileName(context.ModuleDownloadLinks, "ultimateasiloader");
        var autoDetectedNames = context.UalDetectedNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (autoDetectedNames.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(context.UalCachedArchivePath))
            {
                return ComponentInstallStepResult.Skipped(ComponentInstallName.UltimateAsiLoader, "source_not_configured");
            }

            var representative = ResolveRepresentativeName(autoDetectedNames);
            var install = await _dllPayloadInstaller.InstallAsync(
                new DllPayloadInstallRequest
                {
                    TargetPath = context.TargetPath,
                    DestinationRelativePath = representative,
                    SourceDllName = PayloadDllName,
                    Url = url,
                    CachedArchivePath = context.UalCachedArchivePath,
                    DownloadFileName = downloadName
                },
                cancellationToken);

            if (!install.IsSuccess)
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.UltimateAsiLoader, install.ErrorCode);
            }

            foreach (var detectedName in autoDetectedNames)
            {
                if (string.Equals(detectedName, representative, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var existing = Path.Combine(context.TargetPath, detectedName);
                if (!_fileSystem.FileExists(existing))
                {
                    continue;
                }

                InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, existing);
                _fileSystem.DeleteFile(existing);
            }

            return ComponentInstallStepResult.Success(ComponentInstallName.UltimateAsiLoader);
        }

        var fixedTarget = Path.Combine(context.TargetPath, PayloadDllName);
        if (_fileSystem.FileExists(fixedTarget) && !_detectors.IsUltimateAsiLoaderDll(fixedTarget))
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.UltimateAsiLoader, ComponentInstallErrorCodes.InvalidSignature);
        }

        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(context.UalCachedArchivePath))
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.UltimateAsiLoader, ComponentInstallErrorCodes.SourceMissing);
        }

        var result = await _dllPayloadInstaller.InstallAsync(
            new DllPayloadInstallRequest
            {
                TargetPath = context.TargetPath,
                DestinationRelativePath = PayloadDllName,
                SourceDllName = PayloadDllName,
                Url = url,
                CachedArchivePath = context.UalCachedArchivePath,
                DownloadFileName = downloadName
            },
            cancellationToken);

        return result.IsSuccess
            ? ComponentInstallStepResult.Success(ComponentInstallName.UltimateAsiLoader)
            : ComponentInstallStepResult.Failed(ComponentInstallName.UltimateAsiLoader, result.ErrorCode);
    }

    private static string ResolveRepresentativeName(IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            if (string.Equals(name, PayloadDllName, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return names.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? PayloadDllName;
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

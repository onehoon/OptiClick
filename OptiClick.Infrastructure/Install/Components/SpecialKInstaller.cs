using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Components;

public interface ISpecialKInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(SpecialKInstallContext context, CancellationToken cancellationToken = default);
    void CleanupRootSpecialKBeforeProxyResolution(string targetPath, string specialKValue, string preferredProxyDllName);
}

public sealed class SpecialKInstaller : ISpecialKInstaller
{
    private const string SourceDllName = "SpecialK64.dll";
    private readonly IDllPayloadInstaller _dllPayloadInstaller;
    private readonly IInstallFileSystem _fileSystem;
    private readonly IFileSignatureDetectors _detectors;

    public SpecialKInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _dllPayloadInstaller = dllPayloadInstaller ?? throw new ArgumentNullException(nameof(dllPayloadInstaller));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(SpecialKInstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var specialKValue = NormalizeSpecialKValue(context.SpecialKValue);
        if (string.IsNullOrWhiteSpace(specialKValue))
        {
            return ComponentInstallStepResult.Skipped(ComponentInstallName.SpecialK);
        }

        try
        {
            var destination = ResolveDestinationRelativePath(specialKValue, context.FinalDllName);
            if (string.IsNullOrWhiteSpace(destination))
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.SpecialK, ComponentInstallErrorCodes.InvalidDestination);
            }

            CleanupLegacySpecialKFiles(context.TargetPath, destination, context.FinalDllName);

            var url = InstallerExecutionHelpers.ExtractModuleUrl(context.ModuleDownloadLinks, "specialk");
            var downloadName = ReadEntryFileName(context.ModuleDownloadLinks, "specialk");
            var sha256 = InstallerExecutionHelpers.ExtractModuleSha256(context.ModuleDownloadLinks, "specialk");
            var install = await _dllPayloadInstaller.InstallAsync(
                new DllPayloadInstallRequest
                {
                    TargetPath = context.TargetPath,
                    DestinationRelativePath = destination,
                    SourceDllName = SourceDllName,
                    Url = url,
                    CachedArchivePath = context.SpecialKCachedArchivePath,
                    DownloadFileName = downloadName,
                    Sha256 = sha256
                },
                cancellationToken);

            if (!install.IsSuccess)
            {
                return ComponentInstallStepResult.Failed(ComponentInstallName.SpecialK, install.ErrorCode);
            }

            return ComponentInstallStepResult.Success(ComponentInstallName.SpecialK);
        }
        catch (InvalidOperationException)
        {
            return ComponentInstallStepResult.Failed(ComponentInstallName.SpecialK, ComponentInstallErrorCodes.InvalidDestination);
        }
    }

    public void CleanupRootSpecialKBeforeProxyResolution(string targetPath, string specialKValue, string preferredProxyDllName)
    {
        if (!IsPluginsDestination(specialKValue))
        {
            return;
        }

        if (!string.Equals(preferredProxyDllName, "dxgi.dll", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var rootDxgi = Path.Combine(targetPath, "dxgi.dll");
        if (!_fileSystem.FileExists(rootDxgi))
        {
            return;
        }

        if (!_detectors.IsSpecialKDll(rootDxgi))
        {
            return;
        }

        InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, rootDxgi);
        _fileSystem.DeleteFile(rootDxgi);
    }

    private static string NormalizeSpecialKValue(string? value)
    {
        var normalized = (value ?? "").Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized.Trim('/');
    }

    private static bool IsPluginsDestination(string value)
    {
        var normalized = NormalizeSpecialKValue(value);
        return string.Equals(normalized, "plugins", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDestinationRelativePath(string specialKValue, string finalDllName)
    {
        var normalized = NormalizeSpecialKValue(specialKValue);
        if (OptiScalerInstallLayout.IsPluginsToken(normalized))
        {
            var dllName = (finalDllName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dllName)
                || dllName.Contains('/')
                || dllName.Contains('\\')
                || !dllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid final dll name for plugins mode.");
            }

            return OptiScalerInstallLayout.PluginFile(dllName);
        }

        return InstallerExecutionHelpers.NormalizeRelativeDllPath(normalized);
    }

    private void CleanupLegacySpecialKFiles(string targetPath, string destinationRelativePath, string finalDllName)
    {
        var candidates = BuildLegacyCandidates(finalDllName);
        var currentDestination = InstallerExecutionHelpers.CombineUnderTarget(targetPath, destinationRelativePath);
        foreach (var candidateRelative in candidates)
        {
            var candidatePath = InstallerExecutionHelpers.CombineUnderTarget(targetPath, candidateRelative);
            if (string.Equals(candidatePath, currentDestination, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_fileSystem.FileExists(candidatePath))
            {
                continue;
            }

            if (!_detectors.IsSpecialKDll(candidatePath))
            {
                continue;
            }

            InstallerExecutionHelpers.EnsureWritableIfExists(_fileSystem, candidatePath);
            _fileSystem.DeleteFile(candidatePath);
        }
    }

    private static IReadOnlyList<string> BuildLegacyCandidates(string finalDllName)
    {
        var candidates = new List<string>
        {
            "dxgi.dll",
            "plugins/dxgi.dll",
            OptiScalerInstallLayout.PluginFile("dxgi.dll")
        };
        var name = (finalDllName ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(name) && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add($"plugins/{name}");
            candidates.Add(OptiScalerInstallLayout.PluginFile(name));
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ReadEntryFileName(ModuleDownloadLinkCatalog links, string key)
    {
        return links.TryResolveLink(key, out var entry) ? entry.Filename : "";
    }
}

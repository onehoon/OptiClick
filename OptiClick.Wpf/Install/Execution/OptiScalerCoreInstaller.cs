using System.IO;
using OptiClick.Core.Install;
using OptiClick.Wpf.Install.FileSystem;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Install.Planning;
using CoreComponentInstallOperation = OptiClick.Core.Install.Components.ComponentInstallOperation;

namespace OptiClick.Wpf.Install.Execution;

public interface IOptiScalerCoreInstaller
{
    ComponentInstallStepResult Install(ComponentInstallContext context);
}

public sealed class OptiScalerCoreInstaller : IOptiScalerCoreInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.IOptiScalerCoreInstaller _inner;

    public OptiScalerCoreInstaller(
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors,
        IProxyDllNameResolver proxyResolver,
        IAppLogger? logger = null)
    {
        var appLogger = logger ?? NullAppLogger.Instance;
        _inner = new OptiClick.Infrastructure.Install.Components.OptiScalerCoreInstaller(
            new InstallFileSystemAdapter(fileSystem),
            new FileSignatureDetectorsAdapter(detectors),
            new ProxyResolverAdapter(proxyResolver),
            new LoggerAdapter(appLogger));
    }

    public ComponentInstallStepResult Install(ComponentInstallContext context)
    {
        var normalizedTargetPath = InstallTargetPathNormalizer.NormalizeTargetDirectory(context.TargetPath);
        var result = _inner.Install(new OptiClick.Infrastructure.Install.Components.OptiScalerCoreInstallContext
        {
            TargetPath = normalizedTargetPath,
            OptiScalerPayloadDirectory = context.OptiScalerPayloadDirectory,
            FinalDllName = context.FinalDllName,
            PreferredProxyDllName = ResolvePreferredProxyDllName(context),
            // 0.10 OptiScaler payloads are managed as a folder; stale exclude_list entries must not skip files.
            ExcludePatterns = Array.Empty<string>()
        });

        if (!result.IsSuccess)
        {
            return ComponentInstallStepResult.Failed(
                ComponentInstallName.OptiScalerCore,
                result.ErrorCode,
                result.Message);
        }

        return ComponentInstallStepResult.Success(
            ComponentInstallName.OptiScalerCore,
            result.Operations.Select(static operation => new CoreComponentInstallOperation
            {
                Kind = operation.Kind,
                Source = operation.Source,
                Destination = operation.Destination
            }).ToArray());
    }

    private static string ResolvePreferredProxyDllName(ComponentInstallContext context)
    {
        if (context.HasPlannedComponentInstallers)
        {
            return (context.FinalDllName ?? "").Trim();
        }

        return context.ExecutionDescriptor.PreferredProxyDllName;
    }

    private sealed class InstallFileSystemAdapter : OptiClick.Infrastructure.Install.Components.IOptiScalerCoreInstallFileSystem
    {
        private readonly IInstallFileSystem _inner;

        public InstallFileSystemAdapter(IInstallFileSystem inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool FileExists(string path) => _inner.FileExists(path);
        public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
        public void CreateDirectory(string path) => _inner.CreateDirectory(path);
        public void DeleteFile(string path) => _inner.DeleteFile(path);
        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => _inner.CopyFile(sourcePath, destinationPath, overwrite);
        public void MoveFile(string sourcePath, string destinationPath, bool overwrite) => _inner.MoveFile(sourcePath, destinationPath, overwrite);
        public void SetWritable(string path) => _inner.SetWritable(path);
        public bool IsWritable(string path) => _inner.IsWritable(path);
        public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, SearchOption searchOption)
            => _inner.EnumerateFiles(directoryPath, searchPattern, searchOption);
    }

    private sealed class FileSignatureDetectorsAdapter : OptiClick.Infrastructure.Install.Components.IOptiScalerCoreFileSignatureDetectors
    {
        private readonly IFileSignatureDetectors _inner;

        public FileSignatureDetectorsAdapter(IFileSignatureDetectors inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool IsOptiScalerManagedProxyDll(string filePath) => _inner.IsOptiScalerManagedProxyDll(filePath);
    }

    private sealed class ProxyResolverAdapter : OptiClick.Infrastructure.Install.Components.IOptiScalerCoreProxyDllNameResolver
    {
        private readonly IProxyDllNameResolver _inner;

        public ProxyResolverAdapter(IProxyDllNameResolver inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string Resolve(string targetPath, string preferredName) => _inner.Resolve(targetPath, preferredName);
    }

    private sealed class LoggerAdapter : OptiClick.Infrastructure.Install.Components.IOptiScalerCoreInstallLogger
    {
        private readonly IAppLogger _inner;

        public LoggerAdapter(IAppLogger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Info(string category, string message) => _inner.Info(category, message);
        public void Warning(string category, string message) => _inner.Warning(category, message);

        public void Error(string category, string message, Exception? exception = null)
        {
            if (exception is null)
            {
                _inner.Error(category, message);
                return;
            }

            _inner.Error(category, message, exception);
        }
    }
}

public interface IProxyDllNameResolver
{
    string Resolve(string targetPath, string preferredName);
}

public sealed class ProxyDllNameResolver : IProxyDllNameResolver
{
    public const string InvalidTargetFolderErrorCode = "invalid_target_folder";
    public const string InvalidPreferredProxyNameErrorCode = ProxyDllNamePolicy.InvalidPreferredProxyNameErrorCode;
    public const string AsiProxyName = ProxyDllNamePolicy.AsiProxyName;

    private readonly IInstallFileSystem _fileSystem;
    private readonly IFileSignatureDetectors _detectors;

    public ProxyDllNameResolver(IInstallFileSystem fileSystem, IFileSignatureDetectors detectors)
    {
        _fileSystem = fileSystem;
        _detectors = detectors;
    }

    public string Resolve(string targetPath, string preferredName)
    {
        var normalizedTargetPath = (targetPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedTargetPath))
        {
            throw new InvalidOperationException(InvalidTargetFolderErrorCode);
        }

        if (!_fileSystem.DirectoryExists(normalizedTargetPath))
        {
            throw new InvalidOperationException(InvalidTargetFolderErrorCode);
        }

        if (!TryResolveProfilePreferredStart(preferredName, out var canonicalPreferred, out var errorCode))
        {
            throw new InvalidOperationException(errorCode);
        }

        var candidates = ProxyDllNamePolicy.BuildCandidateChainFromCanonical(canonicalPreferred);
        foreach (var candidate in candidates)
        {
            var path = Path.Combine(normalizedTargetPath, candidate);
            if (!_fileSystem.FileExists(path))
            {
                return candidate;
            }

            if (_detectors.IsOptiScalerManagedProxyDll(path))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No available OptiScaler proxy DLL names.");
    }

    public static bool TryResolveProfilePreferredStart(string? preferredName, out string canonicalPreferred, out string errorCode)
    {
        return ProxyDllNamePolicy.TryResolvePreferredStart(preferredName, out canonicalPreferred, out errorCode);
    }

    public static IReadOnlyList<string> BuildCandidateChainForPreferred(string? preferredName)
    {
        return ProxyDllNamePolicy.BuildCandidateChainForPreferred(preferredName);
    }
}

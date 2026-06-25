using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface IFileSignatureDetectors
{
    bool IsOptiScalerManagedProxyDll(string filePath);
    bool IsReShadeDll(string filePath);
    bool IsSpecialKDll(string filePath);
}

public sealed class FileSignatureDetectors : IFileSignatureDetectors
{
    private readonly OptiClick.Infrastructure.FileSystem.FileSignatureDetectors _inner;

    public FileSignatureDetectors(IInstallFileSystem fileSystem)
        : this(new OptiClick.Infrastructure.FileSystem.FileSignatureDetectors(
            (fileSystem ?? throw new ArgumentNullException(nameof(fileSystem))).FileExists))
    {
    }

    internal FileSignatureDetectors(OptiClick.Infrastructure.FileSystem.FileSignatureDetectors inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool IsOptiScalerManagedProxyDll(string filePath) => _inner.IsOptiScalerManagedProxyDll(filePath);

    public bool IsReShadeDll(string filePath) => _inner.IsReShadeDll(filePath);

    public bool IsSpecialKDll(string filePath) => _inner.IsSpecialKDll(filePath);

}

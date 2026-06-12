using OptiClick.Wpf.Install.FileSystem;

namespace OptiClick.Wpf.Install.Execution;

public interface ISpecialKInstaller
{
    Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default);
    void CleanupRootSpecialKBeforeProxyResolution(string targetPath, string specialKValue, string preferredProxyDllName);
}

public sealed class SpecialKInstaller : ISpecialKInstaller
{
    private readonly OptiClick.Infrastructure.Install.Components.SpecialKInstaller _inner;

    public SpecialKInstaller(
        IDllPayloadInstaller dllPayloadInstaller,
        IInstallFileSystem fileSystem,
        IFileSignatureDetectors detectors)
    {
        _inner = new OptiClick.Infrastructure.Install.Components.SpecialKInstaller(
            new DllPayloadInstallerAdapter(dllPayloadInstaller),
            new InstallFileSystemAdapter(fileSystem),
            new FileSignatureDetectorsAdapter(detectors));
    }

    public async Task<ComponentInstallStepResult> InstallAsync(ComponentInstallContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.InstallAsync(
            InfrastructureComponentContextMapper.ToSpecialKContext(context),
            cancellationToken);

        return InfrastructureComponentResultMapper.ToWpfStepResult(result);
    }

    public void CleanupRootSpecialKBeforeProxyResolution(string targetPath, string specialKValue, string preferredProxyDllName)
    {
        _inner.CleanupRootSpecialKBeforeProxyResolution(targetPath, specialKValue, preferredProxyDllName);
    }
}

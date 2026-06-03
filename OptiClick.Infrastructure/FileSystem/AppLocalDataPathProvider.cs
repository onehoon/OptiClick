using System.IO;

namespace OptiClick.Infrastructure.FileSystem;

public interface IAppLocalDataPathProvider
{
    string RootDirectory { get; }
    string LogDirectory { get; }
    string ArchivesDirectory { get; }
    string ManifestDirectory { get; }
    string OptiScalerPayloadDirectory { get; }
    string InstallExecutionTempDirectory { get; }
}

public sealed class AppLocalDataPathProvider : IAppLocalDataPathProvider
{
    public string RootDirectory { get; }

    public string LogDirectory => Path.Combine(RootDirectory, "logs");

    public string ArchivesDirectory => Path.Combine(RootDirectory, "Archives");

    public string ManifestDirectory => Path.Combine(RootDirectory, "Manifest");

    public string OptiScalerPayloadDirectory => Path.Combine(ArchivesDirectory, "optiscaler", "payload");

    public string InstallExecutionTempDirectory => Path.Combine(RootDirectory, "InstallExecution");

    public AppLocalDataPathProvider()
    {
        RootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OptiClick",
            "CacheV2");
    }
}


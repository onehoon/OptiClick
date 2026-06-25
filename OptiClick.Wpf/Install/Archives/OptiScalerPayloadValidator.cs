using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Archives;

public sealed class OptiScalerPayloadValidator : IArchivePayloadValidator
{
    private static readonly string[] RequiredFiles =
    [
        OptiScalerInstallLayout.RootDllFileName,
        OptiScalerInstallLayout.RootIniFileName
    ];

    private static readonly string[] RequiredDirectories =
    [
        OptiScalerInstallLayout.LibraryDirectory
    ];

    public bool IsValid(string payloadDirectory, out string error)
    {
        error = "";
        if (!Directory.Exists(payloadDirectory))
        {
            error = "payload_directory_not_found";
            return false;
        }

        var hasAny = Directory.EnumerateFileSystemEntries(payloadDirectory).Any();
        if (!hasAny)
        {
            error = "payload_directory_empty";
            return false;
        }

        foreach (var fileName in RequiredFiles)
        {
            var path = Path.Combine(payloadDirectory, fileName);
            if (!File.Exists(path))
            {
                error = $"missing_required_payload_file:{fileName}";
                return false;
            }
        }

        foreach (var directoryName in RequiredDirectories)
        {
            var path = Path.Combine(payloadDirectory, directoryName);
            if (!Directory.Exists(path))
            {
                error = $"missing_required_payload_directory:{directoryName}";
                return false;
            }
        }

        return true;
    }
}

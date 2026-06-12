using System.IO;

namespace OptiClick.Infrastructure.Install.Config;

public static class OptionalFileEditRunner
{
    public static void ApplyExistingFileSettings(
        string filePath,
        Action applyCallback,
        bool restoreOriginalReadonly)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var attributes = File.GetAttributes(filePath);
        var originalReadonly = (attributes & FileAttributes.ReadOnly) != 0;
        try
        {
            if (originalReadonly)
            {
                File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            }

            applyCallback();
        }
        finally
        {
            if (originalReadonly && restoreOriginalReadonly && File.Exists(filePath))
            {
                var latestAttributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, latestAttributes | FileAttributes.ReadOnly);
            }
        }
    }
}

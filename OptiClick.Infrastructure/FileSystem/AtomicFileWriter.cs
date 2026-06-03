using System.Text;

namespace OptiClick.Infrastructure.FileSystem;

public static class AtomicFileWriter
{
    public static void WriteAllTextAtomic(string path, string content, Encoding? encoding = null)
    {
        var targetPath = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Target path is required.", nameof(path));
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Target path must include a valid directory.");
        }

        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(
            directory,
            $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        var backupPath = targetPath + ".bak";

        try
        {
            File.WriteAllText(
                tempPath,
                content ?? "",
                encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (File.Exists(targetPath))
            {
                File.Replace(tempPath, targetPath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static string MoveCorruptFile(string path, string suffix = ".corrupt")
    {
        var targetPath = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return "";
        }

        if (!File.Exists(targetPath))
        {
            return "";
        }

        var directory = Path.GetDirectoryName(targetPath) ?? "";
        var fileName = Path.GetFileName(targetPath);
        var safeSuffix = string.IsNullOrWhiteSpace(suffix) ? ".corrupt" : suffix.Trim();
        var destination = Path.Combine(
            directory,
            $"{fileName}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}{safeSuffix}");

        File.Move(targetPath, destination, overwrite: true);
        return destination;
    }
}

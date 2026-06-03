using System.IO;
using System.Linq;
using OptiClick.Infrastructure.Archives;
using OptiClick.Infrastructure.Windows;

namespace OptiClick.Infrastructure.Archives;

public sealed record ArchiveExtractionResult
{
    public static ArchiveExtractionResult Success()
    {
        return new ArchiveExtractionResult
        {
            IsSuccess = true
        };
    }

    public static ArchiveExtractionResult Failure(string errorCode, string errorMessage = "")
    {
        return new ArchiveExtractionResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
}

public static class ArchiveMemberPathSafety
{
    public static bool IsSafeMemberPath(string destinationRoot, string memberPath)
    {
        var root = Path.GetFullPath(destinationRoot ?? "");
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        var normalized = (memberPath ?? "").Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("../", StringComparison.Ordinal)
            || IsDriveAbsolutePath(normalized))
        {
            return false;
        }

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Any(static part => part == ".."))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        return fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
               || fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDriveAbsolutePath(string path)
    {
        return path.Length >= 3
               && char.IsLetter(path[0])
               && path[1] == ':'
               && (path[2] == '\\' || path[2] == '/');
    }
}

public sealed class WindowsTarArchiveExtractor
{
    private readonly IArchiveProcessRunner _processRunner;
    private readonly IOperatingSystemSupportPolicy _operatingSystemSupportPolicy;
    private readonly string _tarExecutable;
    private readonly TimeSpan _timeout;

    public WindowsTarArchiveExtractor(
        IArchiveProcessRunner processRunner,
        IOperatingSystemSupportPolicy operatingSystemSupportPolicy,
        string tarExecutable = "tar.exe",
        TimeSpan? timeout = null)
    {
        _processRunner = processRunner;
        _operatingSystemSupportPolicy = operatingSystemSupportPolicy;
        _tarExecutable = string.IsNullOrWhiteSpace(tarExecutable) ? "tar.exe" : tarExecutable;
        _timeout = timeout ?? TimeSpan.FromSeconds(60);
    }

    public async Task<ArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var osState = _operatingSystemSupportPolicy.Evaluate();
        if (osState.IsUnsupportedWindows10 || !osState.IsSupported)
        {
            return ArchiveExtractionResult.Failure("unsupported_os");
        }

        var source = Path.GetFullPath(archivePath ?? "");
        var destination = Path.GetFullPath(destinationPath ?? "");
        var extension = Path.GetExtension(source).ToLowerInvariant();
        if (extension is not ".zip" and not ".7z")
        {
            return ArchiveExtractionResult.Failure("unsupported_archive_format");
        }

        var tarReady = await _processRunner.RunAsync(_tarExecutable, "--version", _timeout, cancellationToken);
        if (!tarReady.IsSuccess)
        {
            return ArchiveExtractionResult.Failure("tar_missing");
        }

        Directory.CreateDirectory(destination);

        var listArgs = $"-tf \"{source}\"";
        var listed = await _processRunner.RunAsync(_tarExecutable, listArgs, _timeout, cancellationToken);
        if (!listed.IsSuccess)
        {
            return ArchiveExtractionResult.Failure("tar_list_failed");
        }

        var members = listed.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var member in members)
        {
            if (!ArchiveMemberPathSafety.IsSafeMemberPath(destination, member))
            {
                return ArchiveExtractionResult.Failure("unsafe_archive_path");
            }
        }

        var extractArgs = $"-xf \"{source}\" -C \"{destination}\"";
        var extracted = await _processRunner.RunAsync(_tarExecutable, extractArgs, _timeout, cancellationToken);
        if (!extracted.IsSuccess)
        {
            return ArchiveExtractionResult.Failure("tar_extract_failed");
        }

        return ArchiveExtractionResult.Success();
    }
}

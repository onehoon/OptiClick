using System.IO;
using System.IO.Compression;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Install.Archives;

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

    public static ArchiveExtractionResult FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveExtractionResult result)
    {
        return new ArchiveExtractionResult
        {
            IsSuccess = result.IsSuccess,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public interface IArchiveExtractor
{
    Task<ArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default);
}

public static class ArchiveMemberPathSafety
{
    public static bool IsSafeMemberPath(string destinationRoot, string memberPath)
    {
        return OptiClick.Infrastructure.Archives.ArchiveMemberPathSafety.IsSafeMemberPath(destinationRoot, memberPath);
    }
}

public sealed class WindowsTarArchiveExtractor : IArchiveExtractor
{
    private readonly OptiClick.Infrastructure.Archives.WindowsTarArchiveExtractor _inner;

    public WindowsTarArchiveExtractor(
        IArchiveProcessRunner processRunner,
        IOperatingSystemSupportPolicy operatingSystemSupportPolicy,
        string tarExecutable = "tar.exe",
        TimeSpan? timeout = null)
        : this(new OptiClick.Infrastructure.Archives.WindowsTarArchiveExtractor(
            new ArchiveProcessRunnerAdapter(processRunner),
            new OperatingSystemSupportPolicyAdapter(operatingSystemSupportPolicy),
            tarExecutable,
            timeout))
    {
    }

    internal WindowsTarArchiveExtractor(OptiClick.Infrastructure.Archives.WindowsTarArchiveExtractor inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.ExtractAsync(archivePath, destinationPath, cancellationToken);
        return ArchiveExtractionResult.FromInfrastructure(result);
    }

    private sealed class ArchiveProcessRunnerAdapter : OptiClick.Infrastructure.Archives.IArchiveProcessRunner
    {
        private readonly IArchiveProcessRunner _inner;

        public ArchiveProcessRunnerAdapter(IArchiveProcessRunner inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task<OptiClick.Infrastructure.Archives.ArchiveProcessResult> RunAsync(
            string fileName,
            string arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var result = await _inner.RunAsync(fileName, arguments, timeout, cancellationToken);
            return new OptiClick.Infrastructure.Archives.ArchiveProcessResult
            {
                IsSuccess = result.IsSuccess,
                ExitCode = result.ExitCode,
                StandardOutput = result.StandardOutput,
                StandardError = result.StandardError
            };
        }
    }

    private sealed class OperatingSystemSupportPolicyAdapter : OptiClick.Infrastructure.Windows.IOperatingSystemSupportPolicy
    {
        private readonly IOperatingSystemSupportPolicy _inner;

        public OperatingSystemSupportPolicyAdapter(IOperatingSystemSupportPolicy inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public OptiClick.Infrastructure.Windows.OperatingSystemSupportState Evaluate()
        {
            return _inner.Evaluate();
        }
    }
}

public sealed class ZipArchiveExtractor : IArchiveExtractor
{
    public Task<ArchiveExtractionResult> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = Path.GetFullPath(archivePath);
            var destination = Path.GetFullPath(destinationPath);
            var extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension != ".zip")
            {
                return Task.FromResult(ArchiveExtractionResult.Failure("unsupported_archive_format"));
            }

            Directory.CreateDirectory(destination);
            using var archive = ZipFile.OpenRead(source);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FullName))
                {
                    continue;
                }

                if (!ArchiveMemberPathSafety.IsSafeMemberPath(destination, entry.FullName))
                {
                    return Task.FromResult(ArchiveExtractionResult.Failure("unsafe_archive_path"));
                }

                var outputPath = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal)
                    || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(outputPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                entry.ExtractToFile(outputPath, overwrite: true);
            }

            return Task.FromResult(ArchiveExtractionResult.Success());
        }
        catch
        {
            return Task.FromResult(ArchiveExtractionResult.Failure("extract_failed"));
        }
    }
}

using System.Diagnostics;
using System.Text;

namespace OptiClick.Infrastructure.Archives;

public sealed record ArchiveProcessResult
{
    public bool IsSuccess { get; init; }
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = "";
    public string StandardError { get; init; } = "";
}

public interface IArchiveProcessRunner
{
    Task<ArchiveProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class ArchiveProcessRunner : IArchiveProcessRunner
{
    public async Task<ArchiveProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return new ArchiveProcessResult
                {
                    IsSuccess = false,
                    ExitCode = -1
                };
            }

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var token = linkedCts.Token;

            var stdOutTask = process.StandardOutput.ReadToEndAsync(token);
            var stdErrTask = process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);

            return new ArchiveProcessResult
            {
                IsSuccess = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                StandardOutput = await stdOutTask,
                StandardError = await stdErrTask
            };
        }
        catch
        {
            return new ArchiveProcessResult
            {
                IsSuccess = false,
                ExitCode = -1
            };
        }
    }
}



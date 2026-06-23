using System.Diagnostics;
using System.Text;

namespace OptiClick.Infrastructure.Windows;

public interface IWindowsTimeSyncRepairService
{
    Task TryRepairAsync(CancellationToken cancellationToken = default);
}

public sealed class WindowsTimeSyncRepairService : IWindowsTimeSyncRepairService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    private readonly IWindowsTimeSyncCommandRunner _commandRunner;
    private readonly Func<bool> _isWindows;
    private int _attempted;

    public WindowsTimeSyncRepairService()
        : this(new WindowsTimeSyncProcessRunner(), OperatingSystem.IsWindows)
    {
    }

    internal WindowsTimeSyncRepairService(
        IWindowsTimeSyncCommandRunner commandRunner,
        Func<bool>? isWindows = null)
    {
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
        _isWindows = isWindows ?? OperatingSystem.IsWindows;
    }

    public async Task TryRepairAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _attempted, 1) == 1 || !_isWindows())
        {
            return;
        }

        try
        {
            await RunBestEffortAsync("sc.exe", "config w32time start= demand", cancellationToken)
                .ConfigureAwait(false);
            await RunBestEffortAsync("sc.exe", "start w32time", cancellationToken)
                .ConfigureAwait(false);

            var resyncSucceeded = await RunBestEffortAsync("w32tm.exe", "/resync /nowait", cancellationToken)
                .ConfigureAwait(false);
            if (!resyncSucceeded)
            {
                await RunBestEffortAsync("w32tm.exe", "/resync /rediscover /nowait", cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
        }
    }

    private async Task<bool> RunBestEffortAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _commandRunner.RunAsync(
                fileName,
                arguments,
                CommandTimeout,
                cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}

internal interface IWindowsTimeSyncCommandRunner
{
    Task<WindowsTimeSyncCommandResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

internal sealed record WindowsTimeSyncCommandResult
{
    public int ExitCode { get; init; }
}

internal sealed class WindowsTimeSyncProcessRunner : IWindowsTimeSyncCommandRunner
{
    public async Task<WindowsTimeSyncCommandResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Process? process = null;
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

            process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return new WindowsTimeSyncCommandResult { ExitCode = -1 };
            }

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);
            var token = linkedCts.Token;

            var stdoutTask = process.StandardOutput.ReadToEndAsync(token);
            var stderrTask = process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            return new WindowsTimeSyncCommandResult { ExitCode = process.ExitCode };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new WindowsTimeSyncCommandResult { ExitCode = -1 };
        }
        catch
        {
            return new WindowsTimeSyncCommandResult { ExitCode = -1 };
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static void TryKill(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }
}

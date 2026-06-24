using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

public interface IWindowsWmiServiceRecovery
{
    Task<WindowsWmiRecoveryResult> TryStartIfStoppedAsync(CancellationToken cancellationToken = default);
}

public sealed class WindowsWmiServiceRecovery : IWindowsWmiServiceRecovery
{
    private const string LogCategory = "runtime-gpu";
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(8);

    private readonly IWindowsWmiServiceCommandRunner _commandRunner;
    private readonly Func<bool> _isWindows;
    private readonly IAppLogger _logger;
    private int _startAttempted;
    private int _continueAttempted;

    public WindowsWmiServiceRecovery(IAppLogger? logger = null)
        : this(new WindowsWmiServiceProcessRunner(), OperatingSystem.IsWindows, logger)
    {
    }

    internal WindowsWmiServiceRecovery(
        IWindowsWmiServiceCommandRunner commandRunner,
        Func<bool>? isWindows = null,
        IAppLogger? logger = null)
    {
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
        _isWindows = isWindows ?? OperatingSystem.IsWindows;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<WindowsWmiRecoveryResult> TryStartIfStoppedAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_isWindows())
        {
            return LogResult(new WindowsWmiRecoveryResult
            {
                Status = WindowsWmiRecoveryStatuses.SkippedNonWindows
            });
        }

        try
        {
            var scPath = ResolveServiceControlPath();
            var queryResult = await _commandRunner.RunAsync(
                scPath,
                "query winmgmt",
                CommandTimeout,
                cancellationToken).ConfigureAwait(false);
            var queryText = $"{queryResult.StandardOutput}\n{queryResult.StandardError}";
            var serviceStatus = ResolveServiceStatus(queryText);
            if (IsServiceNotFound(queryResult, queryText))
            {
                return LogResult(new WindowsWmiRecoveryResult
                {
                    Status = WindowsWmiRecoveryStatuses.SkippedNotFound,
                    ServiceStatus = serviceStatus
                });
            }

            if (string.Equals(serviceStatus, WindowsWmiServiceStatuses.Running, StringComparison.OrdinalIgnoreCase))
            {
                return LogResult(new WindowsWmiRecoveryResult
                {
                    Status = WindowsWmiRecoveryStatuses.SkippedServiceRunning,
                    ServiceStatus = serviceStatus
                });
            }

            if (string.Equals(serviceStatus, WindowsWmiServiceStatuses.Paused, StringComparison.OrdinalIgnoreCase))
            {
                return await ContinuePausedServiceAsync(
                    scPath,
                    serviceStatus,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!string.Equals(serviceStatus, WindowsWmiServiceStatuses.Stopped, StringComparison.OrdinalIgnoreCase))
            {
                return LogResult(new WindowsWmiRecoveryResult
                {
                    Status = WindowsWmiRecoveryStatuses.StatusUnknown,
                    ServiceStatus = serviceStatus,
                    ErrorType = $"query_exit_{queryResult.ExitCode}"
                });
            }

            if (Interlocked.Exchange(ref _startAttempted, 1) == 1)
            {
                return LogResult(new WindowsWmiRecoveryResult
                {
                    Status = WindowsWmiRecoveryStatuses.SkippedStartAlreadyAttempted,
                    ServiceStatus = serviceStatus
                });
            }

            var startResult = await _commandRunner.RunAsync(
                scPath,
                "start winmgmt",
                CommandTimeout,
                cancellationToken).ConfigureAwait(false);
            var startText = $"{startResult.StandardOutput}\n{startResult.StandardError}";
            if (startResult.ExitCode == 0 || IsAlreadyRunning(startText))
            {
                return LogResult(new WindowsWmiRecoveryResult
                {
                    Status = WindowsWmiRecoveryStatuses.Started,
                    ServiceStatus = WindowsWmiServiceStatuses.Running
                });
            }

            return LogResult(new WindowsWmiRecoveryResult
            {
                Status = IsAccessDenied(startText)
                    ? WindowsWmiRecoveryStatuses.SkippedPermissionDenied
                    : WindowsWmiRecoveryStatuses.StartFailed,
                ServiceStatus = serviceStatus,
                ErrorType = $"start_exit_{startResult.ExitCode}"
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return LogResult(new WindowsWmiRecoveryResult
            {
                Status = WindowsWmiRecoveryStatuses.StartFailed,
                ErrorType = exception.GetType().Name
            });
        }
    }

    private async Task<WindowsWmiRecoveryResult> ContinuePausedServiceAsync(
        string scPath,
        string serviceStatus,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _continueAttempted, 1) == 1)
        {
            return LogResult(new WindowsWmiRecoveryResult
            {
                Status = WindowsWmiRecoveryStatuses.SkippedContinueAlreadyAttempted,
                ServiceStatus = serviceStatus,
                ErrorType = "continue_already_attempted"
            });
        }

        var continueResult = await _commandRunner.RunAsync(
            scPath,
            "continue winmgmt",
            CommandTimeout,
            cancellationToken).ConfigureAwait(false);
        var continueText = $"{continueResult.StandardOutput}\n{continueResult.StandardError}";
        if (continueResult.ExitCode == 0 || IsAlreadyRunning(continueText))
        {
            return LogResult(new WindowsWmiRecoveryResult
            {
                Status = WindowsWmiRecoveryStatuses.Continued,
                ServiceStatus = WindowsWmiServiceStatuses.Running
            });
        }

        return LogResult(new WindowsWmiRecoveryResult
        {
            Status = IsAccessDenied(continueText)
                ? WindowsWmiRecoveryStatuses.SkippedPermissionDenied
                : WindowsWmiRecoveryStatuses.ContinueFailed,
            ServiceStatus = serviceStatus,
            ErrorType = $"continue_exit_{continueResult.ExitCode}"
        });
    }

    private WindowsWmiRecoveryResult LogResult(WindowsWmiRecoveryResult result)
    {
        _logger.Info(
            LogCategory,
            $"wmi recovery status={NormalizeLogValue(result.Status, "unknown")} service_status={NormalizeLogValue(result.ServiceStatus, "unknown")} error_type={NormalizeLogValue(result.ErrorType, "none")}");
        return result;
    }

    private static string ResolveServiceControlPath()
    {
        var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        return string.IsNullOrWhiteSpace(systemDirectory)
            ? "sc.exe"
            : Path.Combine(systemDirectory, "sc.exe");
    }

    private static string ResolveServiceStatus(string text)
    {
        if (Contains(text, "RUNNING") || HasServiceStateCode(text, 4))
        {
            return WindowsWmiServiceStatuses.Running;
        }

        if (Contains(text, "PAUSED") || HasServiceStateCode(text, 7))
        {
            return WindowsWmiServiceStatuses.Paused;
        }

        if (Contains(text, "STOPPED") || HasServiceStateCode(text, 1))
        {
            return WindowsWmiServiceStatuses.Stopped;
        }

        return "";
    }

    private static bool IsServiceNotFound(WindowsWmiServiceCommandResult result, string text)
    {
        return result.ExitCode == 1060
               || Contains(text, "1060")
               || Contains(text, "does not exist")
               || Contains(text, "specified service does not exist");
    }

    private static bool IsAlreadyRunning(string text)
    {
        return Contains(text, "already been started")
               || Contains(text, "already running");
    }

    private static bool IsAccessDenied(string text)
    {
        return Contains(text, "access is denied")
               || Contains(text, "access denied")
               || Contains(text, "error 5");
    }

    private static bool Contains(string source, string value)
    {
        return (source ?? "").Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasServiceStateCode(string source, int stateCode)
    {
        return Regex.IsMatch(
            source ?? "",
            $@":\s*{stateCode}\b",
            RegexOptions.CultureInvariant);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record WindowsWmiRecoveryResult
{
    public string Status { get; init; } = "";
    public string ServiceStatus { get; init; } = "";
    public string ErrorType { get; init; } = "";
}

public static class WindowsWmiRecoveryStatuses
{
    public const string SkippedNonWindows = "skipped_non_windows";
    public const string SkippedServiceRunning = "skipped_service_running";
    public const string SkippedNotFound = "skipped_not_found";
    public const string SkippedPermissionDenied = "skipped_permission_denied";
    public const string SkippedStartAlreadyAttempted = "skipped_start_already_attempted";
    public const string SkippedContinueAlreadyAttempted = "skipped_continue_already_attempted";
    public const string Started = "started";
    public const string Continued = "continued";
    public const string StartFailed = "start_failed";
    public const string ContinueFailed = "continue_failed";
    public const string StatusUnknown = "status_unknown";
}

public static class WindowsWmiServiceStatuses
{
    public const string Running = "running";
    public const string Stopped = "stopped";
    public const string Paused = "paused";
    public const string Disabled = "disabled";
}

internal interface IWindowsWmiServiceCommandRunner
{
    Task<WindowsWmiServiceCommandResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

internal sealed record WindowsWmiServiceCommandResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = "";
    public string StandardError { get; init; } = "";
}

internal sealed class WindowsWmiServiceProcessRunner : IWindowsWmiServiceCommandRunner
{
    public async Task<WindowsWmiServiceCommandResult> RunAsync(
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
                return new WindowsWmiServiceCommandResult { ExitCode = -1 };
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

            return new WindowsWmiServiceCommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdoutTask.Result,
                StandardError = stderrTask.Result
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new WindowsWmiServiceCommandResult { ExitCode = -1 };
        }
        catch
        {
            return new WindowsWmiServiceCommandResult { ExitCode = -1 };
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
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}

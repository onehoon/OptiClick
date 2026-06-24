using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace OptiClick.Infrastructure.Windows;

internal static class WindowsWmiServicePreflight
{
    private static readonly object Gate = new();
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(5);
    private static int _startAttempted;
    private static int _continueAttempted;

    public static WindowsWmiServicePreflightResult EnsureReady(
        TimeSpan commandTimeout = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedNonWindows
            };
        }

        lock (Gate)
        {
            return EnsureReadyCore(commandTimeout <= TimeSpan.Zero ? DefaultCommandTimeout : commandTimeout);
        }
    }

    private static WindowsWmiServicePreflightResult EnsureReadyCore(TimeSpan commandTimeout)
    {
        var scPath = ResolveServiceControlPath();
        var queryResult = RunServiceControl(scPath, "query winmgmt", commandTimeout);
        var queryText = $"{queryResult.StandardOutput}\n{queryResult.StandardError}";
        var serviceStatus = ResolveServiceStatus(queryText);
        if (IsServiceNotFound(queryResult, queryText))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedNotFound,
                ServiceStatus = serviceStatus,
                ErrorType = "service_not_found"
            };
        }

        if (queryResult.ExitCode != 0 && IsAccessDenied(queryText))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedPermissionDenied,
                ServiceStatus = serviceStatus,
                ErrorType = "query_access_denied"
            };
        }

        if (string.Equals(serviceStatus, WindowsWmiServiceStatuses.Running, StringComparison.OrdinalIgnoreCase))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = true,
                Status = WindowsWmiServicePreflightStatuses.Ready,
                ServiceStatus = serviceStatus
            };
        }

        if (string.Equals(serviceStatus, WindowsWmiServiceStatuses.Paused, StringComparison.OrdinalIgnoreCase))
        {
            return ContinuePausedService(scPath, commandTimeout, serviceStatus);
        }

        if (!string.Equals(serviceStatus, WindowsWmiServiceStatuses.Stopped, StringComparison.OrdinalIgnoreCase))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.StatusUnknown,
                ServiceStatus = serviceStatus,
                ErrorType = $"query_exit_{queryResult.ExitCode}"
            };
        }

        var configResult = RunServiceControl(scPath, "qc winmgmt", commandTimeout);
        var configText = $"{configResult.StandardOutput}\n{configResult.StandardError}";
        if (IsServiceDisabled(configText))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedServiceDisabled,
                ServiceStatus = WindowsWmiServiceStatuses.Disabled,
                ErrorType = "service_disabled"
            };
        }

        if (configResult.ExitCode != 0)
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.StatusUnknown,
                ServiceStatus = serviceStatus,
                ErrorType = $"config_exit_{configResult.ExitCode}"
            };
        }

        if (Interlocked.Exchange(ref _startAttempted, 1) == 1)
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedStartAlreadyAttempted,
                ServiceStatus = serviceStatus,
                ErrorType = "start_already_attempted"
            };
        }

        var startResult = RunServiceControl(scPath, "start winmgmt", commandTimeout);
        var startText = $"{startResult.StandardOutput}\n{startResult.StandardError}";
        if (startResult.ExitCode == 0 || IsAlreadyRunning(startText))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = true,
                Status = WindowsWmiServicePreflightStatuses.Started,
                ServiceStatus = WindowsWmiServiceStatuses.Running
            };
        }

        return new WindowsWmiServicePreflightResult
        {
            CanQuery = false,
            Status = IsAccessDenied(startText)
                ? WindowsWmiServicePreflightStatuses.SkippedPermissionDenied
                : WindowsWmiServicePreflightStatuses.StartFailed,
            ServiceStatus = serviceStatus,
            ErrorType = $"start_exit_{startResult.ExitCode}"
        };
    }

    private static WindowsWmiServicePreflightResult ContinuePausedService(
        string scPath,
        TimeSpan commandTimeout,
        string serviceStatus)
    {
        if (Interlocked.Exchange(ref _continueAttempted, 1) == 1)
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = false,
                Status = WindowsWmiServicePreflightStatuses.SkippedContinueAlreadyAttempted,
                ServiceStatus = serviceStatus,
                ErrorType = "continue_already_attempted"
            };
        }

        var continueResult = RunServiceControl(scPath, "continue winmgmt", commandTimeout);
        var continueText = $"{continueResult.StandardOutput}\n{continueResult.StandardError}";
        if (continueResult.ExitCode == 0 || IsAlreadyRunning(continueText))
        {
            return new WindowsWmiServicePreflightResult
            {
                CanQuery = true,
                Status = WindowsWmiServicePreflightStatuses.Continued,
                ServiceStatus = WindowsWmiServiceStatuses.Running
            };
        }

        return new WindowsWmiServicePreflightResult
        {
            CanQuery = false,
            Status = IsAccessDenied(continueText)
                ? WindowsWmiServicePreflightStatuses.SkippedPermissionDenied
                : WindowsWmiServicePreflightStatuses.ContinueFailed,
            ServiceStatus = serviceStatus,
            ErrorType = $"continue_exit_{continueResult.ExitCode}"
        };
    }

    private static WindowsWmiServiceCommandResult RunServiceControl(
        string fileName,
        string arguments,
        TimeSpan timeout)
    {
        Process? process = null;
        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (!process.Start())
            {
                return new WindowsWmiServiceCommandResult { ExitCode = -1 };
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)Math.Max(1, timeout.TotalMilliseconds)))
            {
                TryKill(process);
                return new WindowsWmiServiceCommandResult { ExitCode = -1 };
            }

            Task.WaitAll(stdoutTask, stderrTask);
            return new WindowsWmiServiceCommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdoutTask.Result,
                StandardError = stderrTask.Result
            };
        }
        catch
        {
            TryKill(process);
            return new WindowsWmiServiceCommandResult { ExitCode = -1 };
        }
        finally
        {
            process?.Dispose();
        }
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

    private static bool IsServiceDisabled(string text)
    {
        return Contains(text, "DISABLED") || HasServiceStateCode(text, 4);
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

internal sealed record WindowsWmiServicePreflightResult
{
    public bool CanQuery { get; init; }
    public string Status { get; init; } = "";
    public string ServiceStatus { get; init; } = "";
    public string ErrorType { get; init; } = "";
}

internal static class WindowsWmiServicePreflightStatuses
{
    public const string Ready = "ready";
    public const string Started = "started";
    public const string Continued = "continued";
    public const string SkippedNonWindows = "skipped_non_windows";
    public const string SkippedNotFound = "skipped_not_found";
    public const string SkippedPermissionDenied = "skipped_permission_denied";
    public const string SkippedServiceDisabled = "skipped_service_disabled";
    public const string SkippedStartAlreadyAttempted = "skipped_start_already_attempted";
    public const string SkippedContinueAlreadyAttempted = "skipped_continue_already_attempted";
    public const string StartFailed = "start_failed";
    public const string ContinueFailed = "continue_failed";
    public const string StatusUnknown = "status_unknown";
}

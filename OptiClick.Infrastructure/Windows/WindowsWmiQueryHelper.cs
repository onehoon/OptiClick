using System.Management;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

internal static class WindowsWmiQueryHelper
{
    private static readonly ManagementScope Scope = new(@"\\.\root\cimv2");
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(250);
    private const int DefaultMaxAttempts = 2;

    public static IReadOnlyList<T> Query<T>(
        string query,
        Func<ManagementObject, T?> projector)
        where T : class
    {
        return QueryWithResult(query, projector).Rows;
    }

    public static WindowsWmiQueryResult<T> QueryWithResult<T>(
        string query,
        Func<ManagementObject, T?> projector,
        WindowsWmiQueryOptions? options = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(projector);

        if (!OperatingSystem.IsWindows())
        {
            return new WindowsWmiQueryResult<T>
            {
                Status = WindowsWmiQueryStatuses.NonWindows,
                Attempts = 0
            };
        }

        var effectiveOptions = NormalizeOptions(options);
        var preflight = effectiveOptions.ServicePreflight?.Invoke(effectiveOptions.ServiceCommandTimeout)
                        ?? WindowsWmiServicePreflight.EnsureReady(effectiveOptions.ServiceCommandTimeout);
        LogInfo(
            effectiveOptions,
            $"wmi preflight source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} status={NormalizeLogValue(preflight.Status, "unknown")} service_status={NormalizeLogValue(preflight.ServiceStatus, "unknown")} error_type={NormalizeLogValue(preflight.ErrorType, "none")}");
        if (!preflight.CanQuery)
        {
            return new WindowsWmiQueryResult<T>
            {
                Status = WindowsWmiQueryStatuses.Exception,
                Attempts = 0,
                ErrorType = string.IsNullOrWhiteSpace(preflight.ErrorType)
                    ? preflight.Status
                    : preflight.ErrorType
            };
        }

        var maxAttempts = effectiveOptions.MaxAttempts;
        var lastStatus = WindowsWmiQueryStatuses.Empty;
        var lastErrorType = "";
        var attempts = 0;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            attempts = attempt;
            try
            {
                LogInfo(
                    effectiveOptions,
                    $"wmi query start source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt} timeout_ms={(int)effectiveOptions.Timeout.TotalMilliseconds}");

                var queryTask = StartQueryTask(
                    query,
                    projector,
                    effectiveOptions.Timeout,
                    effectiveOptions.QueryExecutor ?? DefaultWindowsWmiQueryExecutor.Instance);
                if (!WaitForQuery(queryTask, effectiveOptions.Timeout))
                {
                    ObserveFault(queryTask);
                    lastStatus = WindowsWmiQueryStatuses.Timeout;
                    lastErrorType = "HardTimeout";
                    LogWarning(
                        effectiveOptions,
                        $"wmi query failed source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt} status=timeout error_type={NormalizeLogValue(lastErrorType, "none")}");
                }
                else
                {
                    var result = queryTask.GetAwaiter().GetResult();
                    if (result.Count > 0)
                    {
                        LogInfo(
                            effectiveOptions,
                            $"wmi query success source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} rows={result.Count} attempts={attempts}");
                        return new WindowsWmiQueryResult<T>
                        {
                            Rows = result,
                            Status = WindowsWmiQueryStatuses.Success,
                            Attempts = attempts
                        };
                    }

                    lastStatus = WindowsWmiQueryStatuses.Empty;
                    lastErrorType = "";
                    LogWarning(
                        effectiveOptions,
                        $"wmi query empty source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt}");
                }
            }
            catch (ManagementException exception) when (IsTimeout(exception))
            {
                lastStatus = WindowsWmiQueryStatuses.Timeout;
                lastErrorType = exception.GetType().Name;
                LogWarning(
                    effectiveOptions,
                    $"wmi query failed source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt} status=timeout error_type={NormalizeLogValue(lastErrorType, "none")}");
            }
            catch (TimeoutException exception)
            {
                lastStatus = WindowsWmiQueryStatuses.Timeout;
                lastErrorType = exception.GetType().Name;
                LogWarning(
                    effectiveOptions,
                    $"wmi query failed source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt} status=timeout error_type={NormalizeLogValue(lastErrorType, "none")}");
            }
            catch (Exception exception)
            {
                lastStatus = WindowsWmiQueryStatuses.Exception;
                lastErrorType = exception.GetType().Name;
                LogWarning(
                    effectiveOptions,
                    $"wmi query failed source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt} status=exception error_type={NormalizeLogValue(lastErrorType, "none")}");
            }

            if (attempt < maxAttempts)
            {
                LogInfo(
                    effectiveOptions,
                    $"wmi query retry source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} attempt={attempt + 1}");
                Thread.Sleep(effectiveOptions.RetryDelay);
            }
        }

        LogWarning(
            effectiveOptions,
            $"wmi query completed source={NormalizeLogValue(effectiveOptions.SourceName, "unknown")} status={lastStatus} attempts={attempts} error_type={NormalizeLogValue(lastErrorType, "none")}");
        return new WindowsWmiQueryResult<T>
        {
            Status = lastStatus,
            Attempts = attempts,
            ErrorType = lastErrorType
        };
    }

    private static Task<List<T>> StartQueryTask<T>(
        string query,
        Func<ManagementObject, T?> projector,
        TimeSpan timeout,
        IWindowsWmiQueryExecutor executor)
        where T : class
    {
        return Task.Run(() => executor.Execute(query, projector, timeout));
    }

    private sealed class DefaultWindowsWmiQueryExecutor : IWindowsWmiQueryExecutor
    {
        public static DefaultWindowsWmiQueryExecutor Instance { get; } = new();

        private DefaultWindowsWmiQueryExecutor()
        {
        }

        public List<T> Execute<T>(
            string query,
            Func<ManagementObject, T?> projector,
            TimeSpan timeout)
            where T : class
        {
            return ExecuteQuery(query, projector, timeout);
        }
    }

    private static List<T> ExecuteQuery<T>(
        string query,
        Func<ManagementObject, T?> projector,
        TimeSpan timeout)
        where T : class
    {
        using var searcher = new ManagementObjectSearcher(
            Scope,
            new ObjectQuery(query),
            new System.Management.EnumerationOptions
            {
                ReturnImmediately = true,
                Rewindable = false,
                Timeout = timeout
            });

        var result = new List<T>();
        using var collection = searcher.Get();
        foreach (var item in collection.OfType<ManagementObject>())
        {
            using (item)
            {
                var projected = projector(item);
                if (projected is not null)
                {
                    result.Add(projected);
                }
            }
        }

        return result;
    }

    private static bool WaitForQuery<T>(Task<T> task, TimeSpan timeout)
    {
        try
        {
            return task.Wait(timeout);
        }
        catch
        {
            return true;
        }
    }

    private static void ObserveFault(Task task)
    {
        _ = task.ContinueWith(
            static antecedent => _ = antecedent.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public static string ReadString(ManagementBaseObject item, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propertyName);

        return item[propertyName]?.ToString()?.Trim() ?? "";
    }

    private static WindowsWmiQueryOptions NormalizeOptions(WindowsWmiQueryOptions? options)
    {
        var effective = options ?? new WindowsWmiQueryOptions();
        var maxAttempts = effective.MaxAttempts <= 0 ? DefaultMaxAttempts : effective.MaxAttempts;
        var timeout = effective.Timeout <= TimeSpan.Zero ? DefaultTimeout : effective.Timeout;
        var retryDelay = effective.RetryDelay < TimeSpan.Zero ? DefaultRetryDelay : effective.RetryDelay;

        return effective with
        {
            MaxAttempts = maxAttempts,
            Timeout = timeout,
            RetryDelay = retryDelay
        };
    }

    private static bool IsTimeout(ManagementException exception)
    {
        return exception.ErrorCode == ManagementStatus.Timedout
               || exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogInfo(WindowsWmiQueryOptions options, string message)
    {
        options.Logger?.Info(ResolveLogCategory(options), message);
    }

    private static void LogWarning(WindowsWmiQueryOptions options, string message)
    {
        options.Logger?.Warning(ResolveLogCategory(options), message);
    }

    private static string ResolveLogCategory(WindowsWmiQueryOptions options)
    {
        return string.IsNullOrWhiteSpace(options.LogCategory)
            ? "runtime"
            : options.LogCategory.Trim();
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

internal static class WindowsWmiQueryStatuses
{
    public const string Success = "success";
    public const string Empty = "empty";
    public const string Timeout = "timeout";
    public const string Exception = "exception";
    public const string NonWindows = "non_windows";
}

internal sealed record WindowsWmiQueryOptions
{
    public string SourceName { get; init; } = "";
    public string LogCategory { get; init; } = "";
    public int MaxAttempts { get; init; } = 2;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan ServiceCommandTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public IAppLogger? Logger { get; init; }
    public Func<TimeSpan, WindowsWmiServicePreflightResult>? ServicePreflight { get; init; }
    public IWindowsWmiQueryExecutor? QueryExecutor { get; init; }
}

internal interface IWindowsWmiQueryExecutor
{
    List<T> Execute<T>(
        string query,
        Func<ManagementObject, T?> projector,
        TimeSpan timeout)
        where T : class;
}

internal sealed record WindowsWmiQueryResult<T>
    where T : class
{
    public IReadOnlyList<T> Rows { get; init; } = [];
    public string Status { get; init; } = "";
    public int Attempts { get; init; }
    public string ErrorType { get; init; } = "";
}

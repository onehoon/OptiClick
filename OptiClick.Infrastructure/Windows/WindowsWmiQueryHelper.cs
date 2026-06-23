using System.Management;

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
        var maxAttempts = effectiveOptions.MaxAttempts;
        var lastStatus = WindowsWmiQueryStatuses.Empty;
        var lastErrorType = "";
        var attempts = 0;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            attempts = attempt;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    Scope,
                    new ObjectQuery(query),
                    new System.Management.EnumerationOptions
                    {
                        ReturnImmediately = true,
                        Rewindable = false,
                        Timeout = effectiveOptions.Timeout
                    });

                var result = new List<T>();
                foreach (var item in searcher.Get().OfType<ManagementObject>())
                {
                    var projected = projector(item);
                    if (projected is not null)
                    {
                        result.Add(projected);
                    }
                }

                if (result.Count > 0)
                {
                    return new WindowsWmiQueryResult<T>
                    {
                        Rows = result,
                        Status = WindowsWmiQueryStatuses.Success,
                        Attempts = attempts
                    };
                }

                lastStatus = WindowsWmiQueryStatuses.Empty;
                lastErrorType = "";
            }
            catch (ManagementException exception) when (IsTimeout(exception))
            {
                lastStatus = WindowsWmiQueryStatuses.Timeout;
                lastErrorType = exception.GetType().Name;
            }
            catch (TimeoutException exception)
            {
                lastStatus = WindowsWmiQueryStatuses.Timeout;
                lastErrorType = exception.GetType().Name;
            }
            catch (Exception exception)
            {
                lastStatus = WindowsWmiQueryStatuses.Exception;
                lastErrorType = exception.GetType().Name;
            }

            if (attempt < maxAttempts)
            {
                Thread.Sleep(effectiveOptions.RetryDelay);
            }
        }

        return new WindowsWmiQueryResult<T>
        {
            Status = lastStatus,
            Attempts = attempts,
            ErrorType = lastErrorType
        };
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
    public int MaxAttempts { get; init; } = 2;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);
}

internal sealed record WindowsWmiQueryResult<T>
    where T : class
{
    public IReadOnlyList<T> Rows { get; init; } = [];
    public string Status { get; init; } = "";
    public int Attempts { get; init; }
    public string ErrorType { get; init; } = "";
}

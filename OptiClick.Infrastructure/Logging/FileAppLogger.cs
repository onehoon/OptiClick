using System.Globalization;
using System.IO;
using System.Text;

namespace OptiClick.Infrastructure.Logging;

public sealed class FileAppLogger : IAppLogger
{
    private const string LineTimestampFormat = "HH:mm:ss.fff";
    private const string LogLevelEnvironmentVariable = "OPTICLICK_LOG_LEVEL";
    private readonly object _syncRoot = new();
    private readonly ISystemClock _clock;
    private readonly AppLogRetentionPolicy _retentionPolicy;
    private DateTime _lastRetentionRunDate = DateTime.MinValue;

    public FileAppLogger(
        string logDirectory,
        ISystemClock? clock = null,
        AppLogRetentionPolicy? retentionPolicy = null,
        AppLogLevel? minimumLevel = null)
    {
        LogDirectory = (logDirectory ?? "").Trim();
        _clock = clock ?? new SystemClock();
        _retentionPolicy = retentionPolicy ?? new AppLogRetentionPolicy();
        MinimumLevel = minimumLevel ?? ResolveMinimumLevelFromEnvironment();

        try
        {
            EnsureLogDirectory();
            RunRetentionIfNeeded();
        }
        catch
        {
            // Logger initialization must not break application flow.
        }
    }

    public string LogDirectory { get; }

    public AppLogLevel MinimumLevel { get; }

    public void Debug(string category, string message)
    {
        Write(AppLogLevel.Debug, category, message, exception: null);
    }

    public void Info(string category, string message)
    {
        Write(AppLogLevel.Info, category, message, exception: null);
    }

    public void Warning(string category, string message)
    {
        Write(AppLogLevel.Warning, category, message, exception: null);
    }

    public void Error(string category, string message)
    {
        Write(AppLogLevel.Error, category, message, exception: null);
    }

    public void Error(string category, string message, Exception exception)
    {
        Write(AppLogLevel.Error, category, message, exception);
    }

    private void Write(AppLogLevel level, string category, string message, Exception? exception)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        try
        {
            lock (_syncRoot)
            {
                EnsureLogDirectory();
                RunRetentionIfNeeded();

                var now = _clock.Now;
                var logPath = ResolveLogFilePath(now);
                var levelText = level switch
                {
                    AppLogLevel.Debug => "DEBUG",
                    AppLogLevel.Info => "INFO",
                    AppLogLevel.Warning => "WARN",
                    _ => "ERROR"
                };

                var safeCategory = AppLogSanitizer.Sanitize(category);
                var safeMessage = AppLogSanitizer.Sanitize(message);
                var line = $"{now.ToString(LineTimestampFormat, CultureInfo.InvariantCulture)} [{levelText}] [{safeCategory}] {safeMessage}";
                if (exception is not null)
                {
                    var type = AppLogSanitizer.Sanitize(exception.GetType().Name);
                    var exceptionMessage = AppLogSanitizer.Sanitize(exception.Message);
                    line += $" type={type} message={exceptionMessage}";
                }

                AppendLine(logPath, line);
            }
        }
        catch
        {
            // Logging must not break application flow.
        }
    }

    private void RunRetentionIfNeeded()
    {
        var today = _clock.Now.Date;
        if (_lastRetentionRunDate == today)
        {
            return;
        }

        _lastRetentionRunDate = today;
        var cutoff = today.AddDays(-(_retentionPolicy.RetentionDays - 1));

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(LogDirectory, $"{_retentionPolicy.FileNamePrefix}*{_retentionPolicy.FileNameExtension}", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(filePath);
                if (!TryParseLogDate(fileName, out var logDate))
                {
                    continue;
                }

                if (logDate < cutoff)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        // Retention failure should never break app behavior.
                        var now = _clock.Now;
                        var logPath = ResolveLogFilePath(now);
                        var warning = $"{now.ToString(LineTimestampFormat, CultureInfo.InvariantCulture)} [WARN] [log] retention_delete_failed file={AppLogSanitizer.Sanitize(fileName)} type={AppLogSanitizer.Sanitize(ex.GetType().Name)}";
                        AppendLine(logPath, warning);
                    }
                }
            }
        }
        catch
        {
            // Retention failure should never break app behavior.
        }
    }

    private bool TryParseLogDate(string fileName, out DateTime date)
    {
        date = default;
        var prefix = _retentionPolicy.FileNamePrefix;
        var suffix = _retentionPolicy.FileNameExtension;
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var dateText = fileName[prefix.Length..(fileName.Length - suffix.Length)];
        return DateTime.TryParseExact(
            dateText,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private string ResolveLogFilePath(DateTime now)
    {
        var fileName = $"{_retentionPolicy.FileNamePrefix}{now:yyyyMMdd}{_retentionPolicy.FileNameExtension}";
        return Path.Combine(LogDirectory, fileName);
    }

    private static void AppendLine(string path, string line)
    {
        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
    }

    private static AppLogLevel ResolveMinimumLevelFromEnvironment()
    {
        var value = (Environment.GetEnvironmentVariable(LogLevelEnvironmentVariable) ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return AppLogLevel.Info;
        }

        if (string.Equals(value, "warn", StringComparison.OrdinalIgnoreCase))
        {
            return AppLogLevel.Warning;
        }

        return Enum.TryParse<AppLogLevel>(value, ignoreCase: true, out var level)
            ? level
            : AppLogLevel.Info;
    }

    private void EnsureLogDirectory()
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            return;
        }

        Directory.CreateDirectory(LogDirectory);
    }
}

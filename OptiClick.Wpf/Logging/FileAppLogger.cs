namespace OptiClick.Wpf.Logging;

public sealed class FileAppLogger : IAppLogger
{
    private readonly OptiClick.Infrastructure.Logging.FileAppLogger _inner;

    public FileAppLogger(
        string logDirectory,
        ISystemClock? clock = null,
        AppLogRetentionPolicy? retentionPolicy = null,
        AppLogLevel? minimumLevel = null)
    {
        _inner = new OptiClick.Infrastructure.Logging.FileAppLogger(
            logDirectory,
            clock,
            ConvertRetentionPolicy(retentionPolicy),
            ConvertLogLevel(minimumLevel));
    }

    public string LogDirectory => _inner.LogDirectory;

    public void Debug(string category, string message)
    {
        _inner.Debug(category, message);
    }

    public void Info(string category, string message)
    {
        _inner.Info(category, message);
    }

    public void Warning(string category, string message)
    {
        _inner.Warning(category, message);
    }

    public void Error(string category, string message)
    {
        _inner.Error(category, message);
    }

    public void Error(string category, string message, Exception exception)
    {
        _inner.Error(category, message, exception);
    }

    private static OptiClick.Infrastructure.Logging.AppLogRetentionPolicy? ConvertRetentionPolicy(AppLogRetentionPolicy? retentionPolicy)
    {
        if (retentionPolicy is null)
        {
            return null;
        }

        return new OptiClick.Infrastructure.Logging.AppLogRetentionPolicy
        {
            RetentionDays = retentionPolicy.RetentionDays,
            FileNamePrefix = retentionPolicy.FileNamePrefix,
            FileNameExtension = retentionPolicy.FileNameExtension
        };
    }

    private static OptiClick.Infrastructure.Logging.AppLogLevel? ConvertLogLevel(AppLogLevel? level)
    {
        return level switch
        {
            AppLogLevel.Debug => OptiClick.Infrastructure.Logging.AppLogLevel.Debug,
            AppLogLevel.Info => OptiClick.Infrastructure.Logging.AppLogLevel.Info,
            AppLogLevel.Warning => OptiClick.Infrastructure.Logging.AppLogLevel.Warning,
            AppLogLevel.Error => OptiClick.Infrastructure.Logging.AppLogLevel.Error,
            _ => null
        };
    }
}

namespace OptiClick.Wpf.Logging;

public interface IAppLogger : OptiClick.Infrastructure.Logging.IAppLogger
{
}

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    private NullAppLogger()
    {
    }

    public string LogDirectory => "";

    public void Debug(string category, string message)
    {
    }

    public void Info(string category, string message)
    {
    }

    public void Warning(string category, string message)
    {
    }

    public void Error(string category, string message)
    {
    }

    public void Error(string category, string message, Exception exception)
    {
    }
}

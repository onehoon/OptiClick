namespace OptiClick.Infrastructure.Logging;

public interface IAppLogger
{
    string LogDirectory { get; }

    void Info(string category, string message);
    void Warning(string category, string message);
    void Error(string category, string message);
    void Error(string category, string message, Exception exception);
}

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    private NullAppLogger()
    {
    }

    public string LogDirectory => "";

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


namespace OptiClick.Wpf.Logging;

public sealed class AppLogRetentionPolicy
{
    private int _retentionDays = 7;

    public int RetentionDays
    {
        get => _retentionDays;
        init => _retentionDays = value < 1 ? 1 : value;
    }

    public string FileNamePrefix { get; init; } = "opticlick_";
    public string FileNameExtension { get; init; } = ".log";
}

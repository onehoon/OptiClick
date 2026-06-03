namespace OptiClick.Wpf.Logging;

public sealed class SystemClock : ISystemClock
{
    private readonly OptiClick.Infrastructure.Logging.SystemClock _inner = new();

    public DateTime Now => _inner.Now;
}

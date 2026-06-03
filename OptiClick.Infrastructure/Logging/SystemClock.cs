namespace OptiClick.Infrastructure.Logging;

public sealed class SystemClock : ISystemClock
{
    public DateTime Now => DateTime.Now;
}


namespace OptiClick.Infrastructure.Logging;

public interface ISystemClock
{
    DateTime Now { get; }
}


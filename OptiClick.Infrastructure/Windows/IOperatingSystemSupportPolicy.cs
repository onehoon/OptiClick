namespace OptiClick.Infrastructure.Windows;

public interface IOperatingSystemSupportPolicy
{
    OperatingSystemSupportState Evaluate();
}

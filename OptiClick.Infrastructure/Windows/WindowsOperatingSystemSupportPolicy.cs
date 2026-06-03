namespace OptiClick.Infrastructure.Windows;

public sealed class WindowsOperatingSystemSupportPolicy : IOperatingSystemSupportPolicy
{
    private readonly Windows11OnlyOperatingSystemSupportPolicy _inner = new();

    public OperatingSystemSupportState Evaluate() => _inner.Evaluate();
}

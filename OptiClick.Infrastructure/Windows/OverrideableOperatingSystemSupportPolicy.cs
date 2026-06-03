namespace OptiClick.Infrastructure.Windows;

public sealed class OverrideableOperatingSystemSupportPolicy : IOperatingSystemSupportPolicy
{
    public const string WindowsVersionOverrideEnvName = "OPTICLICK_TEST_WINDOWS_VERSION";

    private readonly IOperatingSystemSupportPolicy _fallbackPolicy;
    private readonly Func<string, string?> _environmentReader;

    public OverrideableOperatingSystemSupportPolicy(
        IOperatingSystemSupportPolicy fallbackPolicy,
        Func<string, string?>? environmentReader = null)
    {
        _fallbackPolicy = fallbackPolicy ?? throw new ArgumentNullException(nameof(fallbackPolicy));
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    public OperatingSystemSupportState Evaluate()
    {
        var rawOverride = _environmentReader(WindowsVersionOverrideEnvName);
        var normalized = NormalizeOverride(rawOverride);
        return normalized switch
        {
            10 => OperatingSystemSupportState.UnsupportedWindows10("10 (override)"),
            11 => OperatingSystemSupportState.Supported("11 (override)"),
            _ => _fallbackPolicy.Evaluate()
        };
    }

    private static int? NormalizeOverride(string? rawOverride)
    {
        var normalized = (rawOverride ?? "").Trim();
        if (!int.TryParse(normalized, out var parsed))
        {
            return null;
        }

        return parsed is 10 or 11 ? parsed : null;
    }
}

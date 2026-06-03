namespace OptiClick.Wpf.Configuration;

public sealed record DeviceIdentityRulesOptions
{
    public string Endpoint { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public bool HasEndpoint => !string.IsNullOrWhiteSpace(Endpoint);
}

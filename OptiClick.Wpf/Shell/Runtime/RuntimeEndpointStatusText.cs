using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeEndpointStatusText
{
    public required string RuntimeRemoteEndpointsStatusFormat { get; init; }

    public static RuntimeEndpointStatusText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new RuntimeEndpointStatusText
        {
            RuntimeRemoteEndpointsStatusFormat = strings.RuntimeRemoteEndpointsStatusFormat ?? ""
        };
    }
}

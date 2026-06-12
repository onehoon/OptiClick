using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeCatalogCoordinatorText
{
    public required RuntimeEndpointStatusText EndpointStatusText { get; init; }
    public required RuntimeCatalogFlowText FlowText { get; init; }

    public static RuntimeCatalogCoordinatorText FromAppStrings(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new RuntimeCatalogCoordinatorText
        {
            EndpointStatusText = RuntimeEndpointStatusText.FromAppStrings(strings),
            FlowText = RuntimeCatalogFlowText.FromAppStrings(strings)
        };
    }
}

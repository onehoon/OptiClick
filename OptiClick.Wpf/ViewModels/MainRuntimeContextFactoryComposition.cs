namespace OptiClick.Wpf.ViewModels;

internal static class MainRuntimeContextFactoryComposition
{
    public static MainRuntimeContextFactories Compose(MainRuntimeContextFactoryCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainRuntimeContextFactories
        {
            CatalogUi = new MainRuntimeCatalogUiFlowContextFactory(input.CatalogUi),
            RuntimeFlow = new MainRuntimeFlowContextFactory(input.RuntimeFlow)
        };
    }
}

internal sealed record MainRuntimeContextFactoryCompositionInput
{
    public required MainRuntimeCatalogUiFlowContextFactoryInput CatalogUi { get; init; }
    public required MainRuntimeFlowContextFactoryInput RuntimeFlow { get; init; }
}

internal sealed record MainRuntimeContextFactories
{
    public required MainRuntimeCatalogUiFlowContextFactory CatalogUi { get; init; }
    public required MainRuntimeFlowContextFactory RuntimeFlow { get; init; }
}

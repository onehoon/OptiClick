namespace OptiClick.Wpf.ViewModels;

internal static class MainStartupContextFactoryComposition
{
    public static MainStartupContextFactories Compose(MainStartupContextFactoryCompositionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new MainStartupContextFactories
        {
            StartupFlow = new MainStartupFlowContextFactory(input.StartupFlow),
            StartupPreparation = new StartupPreparationContextFactory(input.StartupPreparation)
        };
    }
}

internal sealed record MainStartupContextFactoryCompositionInput
{
    public required MainStartupFlowContextFactoryInput StartupFlow { get; init; }
    public required StartupPreparationContextFactoryInput StartupPreparation { get; init; }
}

internal sealed record MainStartupContextFactories
{
    public required MainStartupFlowContextFactory StartupFlow { get; init; }
    public required StartupPreparationContextFactory StartupPreparation { get; init; }
}

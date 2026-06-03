using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Composition;

public sealed record MainViewModelFactoryInput
{
    public required MainViewModelRequiredDependencies Required { get; init; }
    public MainViewModelRuntimeDependencies Runtime { get; init; } = new();
    public MainViewModelScanDependencies Scan { get; init; } = new();
    public MainViewModelInstallDependencies Install { get; init; } = new();
    public MainViewModelAppDependencies App { get; init; } = new();
    public bool AllowDependencyFallbacks { get; init; }

    public bool SeedMockGameCards { get; init; }
    public bool SeedMockScanFolders { get; init; }
}

public sealed class MainViewModelFactory
{
    public MainViewModel Create(MainViewModelFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateRequired(input);

        return new MainViewModel(
            input.Required,
            runtime: input.Runtime,
            scan: input.Scan,
            install: input.Install,
            app: input.App,
            allowDependencyFallbacks: input.AllowDependencyFallbacks,
            seedMockGameCards: input.SeedMockGameCards,
            seedMockScanFolders: input.SeedMockScanFolders);
    }

    private static void ValidateRequired(MainViewModelFactoryInput input)
    {
        ArgumentNullException.ThrowIfNull(input.Required);
        ArgumentNullException.ThrowIfNull(input.Required.DialogService);
        ArgumentNullException.ThrowIfNull(input.Required.RuntimeContextProvider);
        ArgumentNullException.ThrowIfNull(input.Required.LanguageProvider);
        ArgumentNullException.ThrowIfNull(input.Required.MockDataProvider);
    }
}

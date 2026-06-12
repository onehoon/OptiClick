using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Composition;

public sealed class MainWindowComposition
{
    public MainViewModel CreateMainViewModel(
        AppSharedServices app,
        RuntimeCompositionServices runtime,
        ScanCompositionServices scan,
        InstallCompositionServices install,
        UpdateCompositionServices update,
        SupportCompositionServices support,
        bool seedMockGameCards = false,
        bool seedMockScanFolders = false)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(install);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(support);

        var viewModelFactory = new MainViewModelFactory();
        return viewModelFactory.Create(
            MainWindowViewModelInputComposer.Compose(
                new MainWindowViewModelInputComposerRequest
                {
                    App = app,
                    Runtime = runtime,
                    Scan = scan,
                    Install = install,
                    Update = update,
                    Support = support,
                    SeedMockGameCards = seedMockGameCards,
                    SeedMockScanFolders = seedMockScanFolders
                }));
    }
}

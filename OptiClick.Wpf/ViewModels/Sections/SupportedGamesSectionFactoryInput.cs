using System.Windows.Input;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Navigation;
using OptiClick.Wpf.Shell.Startup;
using OptiClick.Wpf.Shell.Wiki;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record SupportedGamesSectionFactoryInput
{
    public required ISupportedGamesWikiMarkdownLoader SupportedGamesWikiMarkdownLoader { get; init; }
    public required StartupBackgroundTaskManager StartupBackgroundTaskManager { get; init; }
    public required IAppLogger AppLogger { get; init; }
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required Func<AppLanguage> SelectedLanguageAccessor { get; init; }
    public required Func<ShellViewKind> CurrentViewKindAccessor { get; init; }
    public required Func<ICommand> OpenGameSupportRequestCommandAccessor { get; init; }
    public required Action<Func<StartupPreparationState, StartupPreparationState>> UpdateStartupPreparationState { get; init; }
}

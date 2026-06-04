using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record HomeSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<GameCardViewModel> Games { get; init; }
    public required Func<GameCardViewModel, CancellationToken, Task> SelectGameAsync { get; init; }
    public required Action ShowDetails { get; init; }
    public required Func<CancellationToken, Task> ShowInstallAsync { get; init; }
    public required Func<bool> CanSelectGame { get; init; }
    public required Func<bool> CanShowDetails { get; init; }
    public required Func<bool> CanShowInstall { get; init; }
    public Action<Exception>? OnSelectGameException { get; init; }
    public Action<Exception>? OnShowInstallException { get; init; }
}

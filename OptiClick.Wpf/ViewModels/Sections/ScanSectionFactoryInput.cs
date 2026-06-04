using System.Collections.ObjectModel;
using System.Windows.Media;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Shell.Scan;
using OptiClick.Wpf.ViewModels.Sections.Scan;

namespace OptiClick.Wpf.ViewModels.Sections;

public sealed record ScanSectionFactoryInput
{
    public required Func<AppStrings> StringsAccessor { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> DefaultFolders { get; init; }
    public required ObservableCollection<ScanFolderRowViewModel> AddedFolders { get; init; }
    public required ScanFolderListController ScanFolderListController { get; init; }
    public required ScanFolderActionController ScanFolderActionController { get; init; }
    public required Action<ScanFolderActionResult> ApplyScanFolderActionResult { get; init; }
    public required ScanOrchestrator ScanOrchestrator { get; init; }
    public required Action ShowHome { get; init; }
    public required Brush AddedFolderStatusBrush { get; init; }
    public required Brush MissingFolderStatusBrush { get; init; }
    public Action<Exception>? OnScanCommandException { get; init; }
}

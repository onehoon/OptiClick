using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Scan;

public sealed record ScanFolderStateUpdate
{
    public IReadOnlyList<ScanFolderRowViewModel>? AddedFolders { get; init; }
    public IReadOnlyList<ScanFolderRowViewModel>? DefaultFolders { get; init; }
}

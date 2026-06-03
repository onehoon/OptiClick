using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public interface IShellMockDataProvider
{
    IReadOnlyList<GameCardViewModel> CreateGames();
    IReadOnlyList<ScanFolderRowViewModel> CreateDefaultFolders();
    IReadOnlyList<ScanFolderRowViewModel> CreateAddedFolders();
    ScanFolderRowViewModel CreateAddedFolder(string path);
}

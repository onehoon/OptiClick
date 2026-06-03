using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public interface IScanFolderDiscoveryService
{
    IReadOnlyList<ScanFolderRowViewModel> DiscoverDefaultFolders();
}

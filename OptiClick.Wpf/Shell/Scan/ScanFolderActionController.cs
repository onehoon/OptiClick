using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.Services;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScanFolderActionController
{
    private readonly ScanFolderListController _scanFolderListController;
    private readonly IFolderPickerService? _folderPickerService;
    private readonly ScanFolderDialogPresenter _scanFolderDialogPresenter;

    public ScanFolderActionController(
        ScanFolderListController scanFolderListController,
        IFolderPickerService? folderPickerService,
        ScanFolderDialogPresenter scanFolderDialogPresenter)
    {
        _scanFolderListController = scanFolderListController ?? throw new ArgumentNullException(nameof(scanFolderListController));
        _folderPickerService = folderPickerService;
        _scanFolderDialogPresenter = scanFolderDialogPresenter ?? throw new ArgumentNullException(nameof(scanFolderDialogPresenter));
    }

    public ScanFolderActionResult LoadAddedFoldersFromManifest(
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush,
        Brush missingFolderStatusBrush)
    {
        ArgumentNullException.ThrowIfNull(defaultFolders);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(addedFolderStatusBrush);
        ArgumentNullException.ThrowIfNull(missingFolderStatusBrush);

        Exception? loadException = null;
        var addedFolders = _scanFolderListController.LoadAddedFoldersFromManifest(
            defaultFolders,
            strings,
            addedFolderStatusBrush,
            missingFolderStatusBrush,
            ex => loadException = ex);

        if (loadException is null)
        {
            return new ScanFolderActionResult
            {
                StateUpdate = new ScanFolderStateUpdate
                {
                    AddedFolders = addedFolders
                }
            };
        }

        return new ScanFolderActionResult
        {
            Logs =
            [
                new ScanFlowLogEntry
                {
                    Level = "warning",
                    Category = "scan",
                    Message = $"scan folder manifest load failed type={loadException.GetType().Name}",
                    Exception = loadException
                }
            ],
            StateUpdate = new ScanFolderStateUpdate
            {
                AddedFolders = addedFolders
            }
        };
    }

    public ScanFolderActionResult RelocalizeRows(
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders,
        IReadOnlyCollection<ScanFolderRowViewModel> addedFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush,
        Brush missingFolderStatusBrush)
    {
        ArgumentNullException.ThrowIfNull(defaultFolders);
        ArgumentNullException.ThrowIfNull(addedFolders);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(addedFolderStatusBrush);
        ArgumentNullException.ThrowIfNull(missingFolderStatusBrush);

        _scanFolderListController.RelocalizeRows(
            defaultFolders,
            addedFolders,
            strings,
            addedFolderStatusBrush,
            missingFolderStatusBrush);

        return new ScanFolderActionResult();
    }

    public ScanFolderActionResult RemoveFolder(
        ScanFolderRowViewModel? folder,
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders,
        IReadOnlyCollection<ScanFolderRowViewModel> currentAddedFolders,
        AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(defaultFolders);
        ArgumentNullException.ThrowIfNull(currentAddedFolders);
        ArgumentNullException.ThrowIfNull(strings);

        if (folder is null)
        {
            return new ScanFolderActionResult();
        }

        var targetPath = ScanFolderListController.NormalizePathOrEmpty(folder.Path);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return new ScanFolderActionResult();
        }

        var nextAddedFolders = new List<ScanFolderRowViewModel>(currentAddedFolders.Count);
        var removed = false;

        foreach (var row in currentAddedFolders)
        {
            var rowPath = ScanFolderListController.NormalizePathOrEmpty(row.Path);
            if (!removed && string.Equals(rowPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                removed = true;
                continue;
            }

            nextAddedFolders.Add(row);
        }

        if (!removed)
        {
            return new ScanFolderActionResult();
        }

        _scanFolderListController.SaveFoldersToManifest(defaultFolders, nextAddedFolders);

        return new ScanFolderActionResult
        {
            StatusText = strings.ScanFolderRemoved,
            StateUpdate = new ScanFolderStateUpdate
            {
                AddedFolders = nextAddedFolders
            }
        };
    }

    public ScanFolderActionResult OpenFolder(
        ScanFolderRowViewModel? folder,
        AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        if (folder is null || string.IsNullOrWhiteSpace(folder.Path))
        {
            return new ScanFolderActionResult();
        }

        return new ScanFolderActionResult
        {
            DialogRequest = _scanFolderDialogPresenter.BuildPreviewDialog(folder.Path, strings)
        };
    }

    public ScanFolderActionResult AddFolder(
        IReadOnlyCollection<ScanFolderRowViewModel> defaultFolders,
        IReadOnlyCollection<ScanFolderRowViewModel> currentAddedFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush)
    {
        ArgumentNullException.ThrowIfNull(defaultFolders);
        ArgumentNullException.ThrowIfNull(currentAddedFolders);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(addedFolderStatusBrush);

        if (_folderPickerService is null)
        {
            return new ScanFolderActionResult
            {
                StatusText = strings.ScanFolderPickerMissing,
                Logs =
                [
                    new ScanFlowLogEntry
                    {
                        Level = "warning",
                        Category = "scan",
                        Message = "folder add skipped reason=folder_picker_missing"
                    }
                ]
            };
        }

        var selectedPath = _folderPickerService.PickFolder(strings.ScanAddFolder);
        var defaultRows = new ObservableCollection<ScanFolderRowViewModel>(defaultFolders);
        var addedRows = new ObservableCollection<ScanFolderRowViewModel>(currentAddedFolders);
        var addResult = _scanFolderListController.TryAddFolder(
            selectedPath,
            defaultRows,
            addedRows,
            strings,
            addedFolderStatusBrush);

        var logs = BuildAddFolderLogs(addResult, selectedPath);
        if (addResult.Outcome != ScanFolderAddOutcome.Added)
        {
            return new ScanFolderActionResult
            {
                StatusText = addResult.StatusText,
                Logs = logs
            };
        }

        return new ScanFolderActionResult
        {
            StatusText = addResult.StatusText,
            Logs = logs,
            StateUpdate = new ScanFolderStateUpdate
            {
                AddedFolders = addedRows.ToArray()
            }
        };
    }

    private static IReadOnlyList<ScanFlowLogEntry> BuildAddFolderLogs(ScanFolderAddResult addResult, string? selectedPath)
    {
        switch (addResult.Outcome)
        {
            case ScanFolderAddOutcome.Missing:
                return
                [
                    new ScanFlowLogEntry
                    {
                        Level = "warning",
                        Category = "scan",
                        Message = $"folder add skipped missing path={selectedPath}"
                    }
                ];
            case ScanFolderAddOutcome.NormalizeFailed:
                return
                [
                    new ScanFlowLogEntry
                    {
                        Level = "warning",
                        Category = "scan",
                        Message = "folder add skipped reason=normalize_failed"
                    }
                ];
            case ScanFolderAddOutcome.BlockedBroadPath:
                return
                [
                    new ScanFlowLogEntry
                    {
                        Level = "warning",
                        Category = "scan",
                        Message = $"folder add skipped reason=broad_path_blocked path={selectedPath}"
                    }
                ];
            case ScanFolderAddOutcome.Added:
                return
                [
                    new ScanFlowLogEntry
                    {
                        Level = "info",
                        Category = "scan",
                        Message = $"folder added path={selectedPath}"
                    }
                ];
            default:
                return [];
        }
    }
}

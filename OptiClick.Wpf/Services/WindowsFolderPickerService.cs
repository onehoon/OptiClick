using Microsoft.Win32;

namespace OptiClick.Wpf.Services;

public sealed class WindowsFolderPickerService : IFolderPickerService
{
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = (title ?? "").Trim(),
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return null;
        }

        var folder = (dialog.FolderName ?? "").Trim();
        return string.IsNullOrWhiteSpace(folder) ? null : folder;
    }
}

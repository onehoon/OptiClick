using System.Collections.ObjectModel;
using System.Windows.Media;
using OptiClick.Core.Scan;
using OptiClick.Infrastructure.Scan;
using OptiClick.Wpf.Localization;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Shell.Scan;

public sealed class ScanFolderListController
{
    private readonly IScanFolderManifestStore? _scanFolderManifestStore;
    private readonly IScanFileSystemProbe _fileSystemProbe;

    public ScanFolderListController(
        IScanFolderManifestStore? scanFolderManifestStore = null,
        IScanFileSystemProbe? fileSystemProbe = null)
    {
        _scanFolderManifestStore = scanFolderManifestStore;
        _fileSystemProbe = fileSystemProbe ?? new ScanFileSystemProbe();
    }

    public IReadOnlyList<ScanFolderRowViewModel> LoadAddedFoldersFromManifest(
        IEnumerable<ScanFolderRowViewModel> defaultFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush,
        Brush missingFolderStatusBrush,
        Action<Exception>? onLoadFailed = null)
    {
        if (_scanFolderManifestStore is null)
        {
            return [];
        }

        IReadOnlyList<ScanFolderManifestEntry> manifestEntries;
        try
        {
            manifestEntries = _scanFolderManifestStore.Load();
        }
        catch (Exception ex)
        {
            onLoadFailed?.Invoke(ex);
            return [];
        }

        if (manifestEntries.Count == 0)
        {
            return [];
        }

        ApplySavedDefaultFolderStates(defaultFolders, manifestEntries);
        var defaultPaths = new HashSet<string>(
            defaultFolders
                .Select(static folder => NormalizePathOrEmpty(folder.Path))
                .Where(static path => !string.IsNullOrWhiteSpace(path)),
            StringComparer.OrdinalIgnoreCase);
        var rows = new List<ScanFolderRowViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifestEntries)
        {
            var normalizedPath = NormalizePathOrEmpty(entry.Path);
            if (string.IsNullOrWhiteSpace(normalizedPath)
                || entry.IsDefault
                || ScanFolderPathPolicy.IsBlockedBroadPath(normalizedPath)
                || seen.Contains(normalizedPath)
                || defaultPaths.Contains(normalizedPath))
            {
                continue;
            }

            seen.Add(normalizedPath);
            var exists = _fileSystemProbe.DirectoryExists(normalizedPath);
            rows.Add(new ScanFolderRowViewModel(
                strings.ScanCustomFolderLabel,
                normalizedPath,
                exists ? strings.ScanAddedStatusLabel : strings.ScanMissingStatusLabel,
                exists && entry.IsChecked,
                exists,
                true,
                exists ? addedFolderStatusBrush : missingFolderStatusBrush));
        }

        return rows;
    }

    public ScanFolderAddResult TryAddFolder(
        string? selectedPath,
        ObservableCollection<ScanFolderRowViewModel> defaultFolders,
        ObservableCollection<ScanFolderRowViewModel> addedFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush)
    {
        var trimmedPath = (selectedPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            return new ScanFolderAddResult
            {
                Outcome = ScanFolderAddOutcome.Cancelled,
                StatusText = strings.ScanFolderSelectionCancelled
            };
        }

        if (!_fileSystemProbe.DirectoryExists(trimmedPath))
        {
            return new ScanFolderAddResult
            {
                Outcome = ScanFolderAddOutcome.Missing,
                StatusText = strings.ScanSelectedFolderMissing
            };
        }

        var normalizedSelectedPath = NormalizePathOrEmpty(trimmedPath);
        if (string.IsNullOrWhiteSpace(normalizedSelectedPath))
        {
            return new ScanFolderAddResult
            {
                Outcome = ScanFolderAddOutcome.NormalizeFailed,
                StatusText = strings.ScanSelectedFolderMissing
            };
        }

        if (ScanFolderPathPolicy.IsBlockedBroadPath(normalizedSelectedPath))
        {
            return new ScanFolderAddResult
            {
                Outcome = ScanFolderAddOutcome.BlockedBroadPath,
                StatusText = strings.ScanSelectedFolderTooBroad
            };
        }

        var alreadyExists = defaultFolders
            .Concat(addedFolders)
            .Any(folder => string.Equals(NormalizePathOrEmpty(folder.Path), normalizedSelectedPath, StringComparison.OrdinalIgnoreCase));
        if (alreadyExists)
        {
            return new ScanFolderAddResult
            {
                Outcome = ScanFolderAddOutcome.Duplicate,
                StatusText = strings.ScanFolderAlreadyAdded
            };
        }

        addedFolders.Add(new ScanFolderRowViewModel(
            strings.ScanCustomFolderLabel,
            normalizedSelectedPath,
            strings.ScanAddedStatusLabel,
            true,
            true,
            true,
            addedFolderStatusBrush));
        SaveFoldersToManifest(defaultFolders, addedFolders);

        return new ScanFolderAddResult
        {
            Outcome = ScanFolderAddOutcome.Added,
            StatusText = strings.ScanFolderAdded,
            NormalizedPath = normalizedSelectedPath
        };
    }

    public bool RemoveFolder(ObservableCollection<ScanFolderRowViewModel> addedFolders, ScanFolderRowViewModel folder)
    {
        if (!addedFolders.Remove(folder))
        {
            return false;
        }

        SaveAddedFoldersToManifest(addedFolders);
        return true;
    }

    public void RelocalizeRows(
        IEnumerable<ScanFolderRowViewModel> defaultFolders,
        IEnumerable<ScanFolderRowViewModel> addedFolders,
        AppStrings strings,
        Brush addedFolderStatusBrush,
        Brush missingFolderStatusBrush)
    {
        foreach (var folder in addedFolders)
        {
            var exists = _fileSystemProbe.DirectoryExists(folder.Path);
            folder.ApplyLocalization(
                strings.ScanCustomFolderLabel,
                exists ? strings.ScanAddedStatusLabel : strings.ScanMissingStatusLabel,
                exists,
                exists ? addedFolderStatusBrush : missingFolderStatusBrush);
        }

        foreach (var folder in defaultFolders)
        {
            var exists = _fileSystemProbe.DirectoryExists(folder.Path);
            folder.ApplyLocalization(
                name: null,
                status: exists ? strings.ScanDetectedStatusLabel : strings.ScanMissingStatusLabel,
                canOpen: exists,
                statusBackground: exists ? addedFolderStatusBrush : missingFolderStatusBrush);
        }
    }

    public string[] ResolveScanFolders(
        IEnumerable<ScanFolderRowViewModel> defaultFolders,
        IEnumerable<ScanFolderRowViewModel> addedFolders)
    {
        return defaultFolders
            .Concat(addedFolders)
            .Where(static folder => folder.IsChecked)
            .Select(static folder => NormalizePathOrEmpty(folder.Path))
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Where(static path => ScanFolderPathPolicy.IsAllowedScanFolderPath(path))
            .Where(_fileSystemProbe.DirectoryExists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void SaveFoldersToManifest(
        IEnumerable<ScanFolderRowViewModel> defaultFolders,
        IEnumerable<ScanFolderRowViewModel> addedFolders)
    {
        if (_scanFolderManifestStore is null)
        {
            return;
        }

        var entries = BuildManifestEntries(defaultFolders, isDefault: true)
            .Concat(BuildManifestEntries(addedFolders, isDefault: false))
            .ToArray();

        _scanFolderManifestStore.Save(entries);
    }

    public void SaveAddedFoldersToManifest(IEnumerable<ScanFolderRowViewModel> addedFolders)
    {
        if (_scanFolderManifestStore is null)
        {
            return;
        }

        var entries = BuildManifestEntries(addedFolders, isDefault: false).ToArray();
        _scanFolderManifestStore.Save(entries);
    }

    private static IEnumerable<ScanFolderManifestEntry> BuildManifestEntries(
        IEnumerable<ScanFolderRowViewModel> folders,
        bool isDefault)
    {
        return folders
            .Select(folder => new ScanFolderManifestEntry
            {
                Path = NormalizePathOrEmpty(folder.Path),
                IsChecked = folder.IsChecked,
                IsDefault = isDefault,
                AddedAt = DateTimeOffset.Now
            })
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Path))
            .Where(static entry => ScanFolderPathPolicy.IsAllowedScanFolderPath(entry.Path));
    }

    private static void ApplySavedDefaultFolderStates(
        IEnumerable<ScanFolderRowViewModel> defaultFolders,
        IReadOnlyList<ScanFolderManifestEntry> manifestEntries)
    {
        var savedByPath = manifestEntries
            .Select(entry => new
            {
                Path = NormalizePathOrEmpty(entry.Path),
                entry.IsChecked
            })
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Path))
            .GroupBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().IsChecked,
                StringComparer.OrdinalIgnoreCase);

        foreach (var folder in defaultFolders)
        {
            var normalizedPath = NormalizePathOrEmpty(folder.Path);
            if (string.IsNullOrWhiteSpace(normalizedPath)
                || !savedByPath.TryGetValue(normalizedPath, out var isChecked))
            {
                continue;
            }

            folder.IsChecked = folder.CanOpen && isChecked;
        }
    }

    public static string NormalizePathOrEmpty(string? path) => ScanFolderPathPolicy.NormalizePathOrEmpty(path);
}

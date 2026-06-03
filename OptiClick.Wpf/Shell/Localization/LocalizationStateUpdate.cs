namespace OptiClick.Wpf.Shell.Localization;

public sealed record LocalizationStateUpdate
{
    public string SelectedLanguageDisplayName { get; init; } = "";
    public string SettingsStatusText { get; init; } = "";
    public string ScanStatusText { get; init; } = "";
    public string DeviceText { get; init; } = "";
    public string GpuText { get; init; } = "";
    public bool ShouldRelocalizeScanFolders { get; init; }
    public bool ShouldRefreshRuntimeSummary { get; init; }
}

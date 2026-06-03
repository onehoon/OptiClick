namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchivePreparationOptions
{
    public TimeSpan DownloadTimeout { get; init; } = TimeSpan.FromSeconds(300);
    public bool ValidateZipFiles { get; init; } = true;
    public bool CleanupStaleFiles { get; init; } = true;
}

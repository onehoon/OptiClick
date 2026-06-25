namespace OptiClick.Core.Models;

public sealed record ArchiveState
{
    public string OptiScalerArchive { get; init; } = "";
    public bool OptiScalerReady { get; init; } = true;
    public bool OptiScalerDownloading { get; init; }
    public bool ShouldInstallFsr4 { get; init; }
    public bool Fsr4Ready { get; init; } = true;
    public bool Fsr4Downloading { get; init; }
    public bool ExtraBundleReady { get; init; } = true;
    public bool ReframeworkReady { get; init; } = true;
    public bool SpecialKReady { get; init; } = true;
    public bool Unreal5Ready { get; init; } = true;
    public bool OptiPatcherReady { get; init; } = true;
}

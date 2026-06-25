namespace OptiClick.Core.Models;

public enum DllOwnerKind
{
    Unknown,
    None,
    OptiScaler,
    ReShade,
    SpecialK,
    RenoDx,
    Other
}

public sealed record ExistingFileEntry
{
    public string RelativePath { get; init; } = "";
    public bool Exists { get; init; }
    public DllOwnerKind OwnerKind { get; init; } = DllOwnerKind.Unknown;
    public string OriginalFilename { get; init; } = "";
}

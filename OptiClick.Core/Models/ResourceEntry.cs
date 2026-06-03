namespace OptiClick.Core.Models;

public sealed record ResourceEntry
{
    public string ResourceId { get; init; } = "";
    public string ResourceGroup { get; init; } = "";
    public string Name { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Url { get; init; } = "";
    public string Alias { get; init; } = "";
    public string BundleKey { get; init; } = "";
}

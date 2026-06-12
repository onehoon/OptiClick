namespace OptiClick.Core.Games.Wiki;

public sealed record SupportedGamesWikiEntry
{
    public string GameId { get; init; } = "";
    public IReadOnlyList<string> GameIds { get; init; } = [];
    public string GameNameEn { get; init; } = "";
    public string GameNameKr { get; init; } = "";
    public string CoverSteamAppId { get; init; } = "";
    public IReadOnlyList<string> CoverSteamAppIds { get; init; } = [];
    public string CoverUrl { get; init; } = "";
    public IReadOnlyList<string> CoverUrls { get; init; } = [];
    public string IntelText { get; init; } = "";
    public string AmdText { get; init; } = "";
    public string NvidiaText { get; init; } = "";
    public bool IsNewlySupported { get; init; }
}

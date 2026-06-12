namespace OptiClick.Core.RuntimeData;

public sealed class RemoteRuntimeData
{
    public static readonly RemoteRuntimeData Empty = new();
    private IReadOnlyList<RuntimeDataGameProfile> _gameMaster = [];

    public int SchemaVersion { get; init; }
    public string GeneratedAt { get; init; } = "";
    public IReadOnlyList<RuntimeDataGameProfile> GameMaster
    {
        get => _gameMaster;
        init => _gameMaster = value ?? [];
    }
    public IReadOnlyList<RuntimeDataProfileRow> GameIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameUnrealIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameXmlProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> GameJsonProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> EngineIniProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataProfileRow> RegistryProfile { get; init; } = [];
    public IReadOnlyList<RuntimeDataResourceRow> ResourceMaster { get; init; } = [];
    public IReadOnlyList<RuntimeDataMessageRow> MessageBinding { get; init; } = [];
    public IReadOnlyList<RuntimeDataMessageRow> MessageCenter { get; init; } = [];
    public IReadOnlyList<RuntimeDataNewGameSupportRow> NewGameSupport { get; init; } = [];

    public IReadOnlyList<RuntimeDataGameProfile> Games
    {
        get => _gameMaster;
        init => _gameMaster = value ?? [];
    }
}

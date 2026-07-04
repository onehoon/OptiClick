namespace OptiClick.Core.Runtime;

public sealed record AuthV2Credential
{
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";

    public bool IsUsable =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}

public interface IAuthV2CredentialStore
{
    AuthV2Credential? Load();
    void Save(AuthV2Credential credential);
    void Clear();
}


using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Storage;

public sealed class AuthV2CredentialStore : IAuthV2CredentialStore
{
    private const string CredentialFileName = "auth-v2-credential.json";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("OptiClick.AuthV2Credential.v1");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _credentialPath;
    private readonly IAppLogger _logger;

    public AuthV2CredentialStore(
        IAppLocalDataPathProvider? pathProvider = null,
        IAppLogger? logger = null)
    {
        var provider = pathProvider ?? new AppLocalDataPathProvider();
        Directory.CreateDirectory(provider.ManifestDirectory);
        _credentialPath = Path.Combine(provider.ManifestDirectory, CredentialFileName);
        _logger = logger ?? NullAppLogger.Instance;
    }

    public AuthV2Credential? Load()
    {
        if (!File.Exists(_credentialPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_credentialPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredCredential>(json);
            if (stored is null || string.IsNullOrWhiteSpace(stored.ClientId) || string.IsNullOrWhiteSpace(stored.ProtectedClientSecret))
            {
                return null;
            }

            var secretBytes = Convert.FromBase64String(stored.ProtectedClientSecret);
            var unprotected = ProtectedData.Unprotect(secretBytes, Entropy, DataProtectionScope.CurrentUser);
            var secret = Encoding.UTF8.GetString(unprotected);
            var credential = new AuthV2Credential
            {
                ClientId = stored.ClientId.Trim(),
                ClientSecret = secret.Trim()
            };

            return credential.IsUsable ? credential : null;
        }
        catch (Exception ex)
        {
            _logger.Warning("remote-v2", $"auth v2 credential load failed type={ex.GetType().Name}");
            return null;
        }
    }

    public void Save(AuthV2Credential credential)
    {
        if (credential?.IsUsable != true)
        {
            return;
        }

        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(credential.ClientSecret.Trim());
            var protectedBytes = ProtectedData.Protect(secretBytes, Entropy, DataProtectionScope.CurrentUser);
            var stored = new StoredCredential
            {
                ClientId = credential.ClientId.Trim(),
                ProtectedClientSecret = Convert.ToBase64String(protectedBytes)
            };
            var json = JsonSerializer.Serialize(stored, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_credentialPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning("remote-v2", $"auth v2 credential save failed type={ex.GetType().Name}");
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_credentialPath))
            {
                File.Delete(_credentialPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning("remote-v2", $"auth v2 credential clear failed type={ex.GetType().Name}");
        }
    }

    private sealed record StoredCredential
    {
        public string ClientId { get; init; } = "";
        public string ProtectedClientSecret { get; init; } = "";
    }
}


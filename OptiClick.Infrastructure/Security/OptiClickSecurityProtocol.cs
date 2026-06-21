using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OptiClick.Infrastructure.FileSystem;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Security;

public static class OptiClickProtocolHeaders
{
    public const string Protocol = "X-OptiClick-Protocol";
    public const string AppVersion = "X-OptiClick-App-Version";
    public const string AppStartId = "X-OptiClick-App-Start-Id";
    public const string ClientId = "X-OptiClick-Client-Id";
    public const string Timestamp = "X-OptiClick-Timestamp";
    public const string Nonce = "X-OptiClick-Nonce";
    public const string Signature = "X-OptiClick-Signature";
    public const string BundleTicket = "X-OptiClick-Bundle-Ticket";
    public const string DownloadTicket = "X-OptiClick-Download-Ticket";
}

public sealed record OptiClickClientCredential
{
    public int SchemaVersion { get; init; } = 1;
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public DateTimeOffset CreatedAtUtc { get; init; }
}

internal sealed record StoredOptiClickClientCredential
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = "";

    [JsonPropertyName("protected_client_secret")]
    public string ProtectedClientSecret { get; init; } = "";

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAtUtc { get; init; }

    [JsonPropertyName("last_registered_app_version")]
    public string LastRegisteredAppVersion { get; init; } = "";
}

public interface IOptiClickClientCredentialStore
{
    Task<OptiClickClientCredential?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        OptiClickClientCredential credential,
        string appVersion,
        CancellationToken cancellationToken = default);
}

public interface IOptiClickSecretProtector
{
    byte[] Protect(byte[] plainBytes);

    byte[] Unprotect(byte[] protectedBytes);
}

public sealed class DpapiOptiClickSecretProtector : IOptiClickSecretProtector
{
    public byte[] Protect(byte[] plainBytes)
    {
        return ProtectedData.Protect(
            plainBytes ?? [],
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);
    }

    public byte[] Unprotect(byte[] protectedBytes)
    {
        return ProtectedData.Unprotect(
            protectedBytes ?? [],
            optionalEntropy: null,
            DataProtectionScope.CurrentUser);
    }
}

public sealed class ProtectedDataOptiClickClientCredentialStore : IOptiClickClientCredentialStore
{
    private const string StoreFileName = "client-credential.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _credentialPath;
    private readonly IOptiClickSecretProtector _secretProtector;
    private readonly IAppLogger _logger;

    public ProtectedDataOptiClickClientCredentialStore(
        string? credentialPath = null,
        IOptiClickSecretProtector? secretProtector = null,
        IAppLogger? logger = null)
    {
        _credentialPath = string.IsNullOrWhiteSpace(credentialPath)
            ? BuildDefaultCredentialPath()
            : Path.GetFullPath(credentialPath);
        _secretProtector = secretProtector ?? new DpapiOptiClickSecretProtector();
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<OptiClickClientCredential?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(_credentialPath);
        _logger.Debug("Security", $"client credential load start file={fileName}");
        if (!File.Exists(_credentialPath))
        {
            _logger.Info("Security", $"client credential load skipped reason=file_missing file={fileName}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_credentialPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.Info("Security", $"client credential load skipped reason=empty_file file={fileName}");
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredOptiClickClientCredential>(json, SerializerOptions);
            if (stored is null
                || stored.SchemaVersion != 1
                || string.IsNullOrWhiteSpace(stored.ClientId)
                || string.IsNullOrWhiteSpace(stored.ProtectedClientSecret))
            {
                var movedPath = MoveCorruptCredentialFile();
                _logger.Warning(
                    "Security",
                    $"client credential load invalid reason=invalid_payload file={fileName} moved={Path.GetFileName(movedPath)}");
                return null;
            }

            var protectedBytes = Convert.FromBase64String(stored.ProtectedClientSecret);
            var secretBytes = _secretProtector.Unprotect(protectedBytes);
            var clientSecret = Encoding.UTF8.GetString(secretBytes);
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                var movedPath = MoveCorruptCredentialFile();
                _logger.Warning(
                    "Security",
                    $"client credential load invalid reason=empty_secret file={fileName} moved={Path.GetFileName(movedPath)}");
                return null;
            }

            _logger.Info(
                "Security",
                $"client credential load success file={fileName} client_id_present=true created_at_present={FormatBool(stored.CreatedAtUtc != default)}");
            return new OptiClickClientCredential
            {
                SchemaVersion = stored.SchemaVersion,
                ClientId = stored.ClientId.Trim(),
                ClientSecret = clientSecret.Trim(),
                CreatedAtUtc = stored.CreatedAtUtc
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var movedPath = MoveCorruptCredentialFile();
            _logger.Warning(
                "Security",
                $"client credential load failed file={Path.GetFileName(_credentialPath)} type={ex.GetType().Name} moved={Path.GetFileName(movedPath)}");
            return null;
        }
    }

    public Task SaveAsync(
        OptiClickClientCredential credential,
        string appVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fileName = Path.GetFileName(_credentialPath);
        if (credential is null
            || string.IsNullOrWhiteSpace(credential.ClientId)
            || string.IsNullOrWhiteSpace(credential.ClientSecret))
        {
            _logger.Info("Security", $"client credential save skipped reason=invalid_credential file={fileName}");
            return Task.CompletedTask;
        }

        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(credential.ClientSecret.Trim());
            var protectedBytes = _secretProtector.Protect(secretBytes);
            var stored = new StoredOptiClickClientCredential
            {
                SchemaVersion = 1,
                ClientId = credential.ClientId.Trim(),
                ProtectedClientSecret = Convert.ToBase64String(protectedBytes),
                CreatedAtUtc = credential.CreatedAtUtc == default
                    ? DateTimeOffset.UtcNow
                    : credential.CreatedAtUtc,
                LastRegisteredAppVersion = (appVersion ?? "").Trim()
            };

            var json = JsonSerializer.Serialize(stored, SerializerOptions);
            AtomicFileWriter.WriteAllTextAtomic(_credentialPath, json);
            _logger.Info(
                "Security",
                $"client credential save success file={fileName} app_version={NormalizeLogValue(appVersion, "none")}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warning(
                "Security",
                $"client credential save failed file={fileName} type={ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }

    private string MoveCorruptCredentialFile()
    {
        try
        {
            return AtomicFileWriter.MoveCorruptFile(_credentialPath);
        }
        catch
        {
            return "";
        }
    }

    private static string BuildDefaultCredentialPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OptiClick",
            "Security",
            StoreFileName);
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record OptiClickClientRegistrationResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; init; } = "";

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public enum OptiClickClientRegistrationFailureKind
{
    None = 0,
    Unsupported = 1,
    TransientFailure = 2
}

public sealed record OptiClickClientRegistrationResult
{
    public OptiClickClientCredential? Credential { get; init; }
    public OptiClickClientRegistrationFailureKind FailureKind { get; init; }

    public bool IsSuccess => Credential is not null;
    public bool IsUnsupported => FailureKind == OptiClickClientRegistrationFailureKind.Unsupported;

    public static OptiClickClientRegistrationResult Success(OptiClickClientCredential credential)
    {
        return new OptiClickClientRegistrationResult
        {
            Credential = credential
        };
    }

    public static OptiClickClientRegistrationResult Unsupported()
    {
        return new OptiClickClientRegistrationResult
        {
            FailureKind = OptiClickClientRegistrationFailureKind.Unsupported
        };
    }

    public static OptiClickClientRegistrationResult TransientFailure()
    {
        return new OptiClickClientRegistrationResult
        {
            FailureKind = OptiClickClientRegistrationFailureKind.TransientFailure
        };
    }
}

public interface IOptiClickClientRegistrationClient
{
    Task<OptiClickClientRegistrationResult> RegisterAsync(CancellationToken cancellationToken = default);
}

public sealed class OptiClickClientRegistrationClient : IOptiClickClientRegistrationClient
{
    private const string UserAgentProduct = "OptiClick";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri? _registrationEndpoint;
    private readonly Func<string?> _appVersionProvider;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _timeout;

    public OptiClickClientRegistrationClient(
        HttpClient httpClient,
        Uri? registrationEndpoint,
        Func<string?> appVersionProvider,
        IAppLogger? logger = null,
        TimeSpan? timeout = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _registrationEndpoint = registrationEndpoint;
        _appVersionProvider = appVersionProvider ?? (() => "");
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<OptiClickClientRegistrationResult> RegisterAsync(CancellationToken cancellationToken = default)
    {
        if (_registrationEndpoint is null)
        {
            _logger.Info("Security", "client registration skipped reason=endpoint_missing");
            return OptiClickClientRegistrationResult.Unsupported();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            _logger.Info(
                "Security",
                $"client registration request start endpoint_host={NormalizeLogValue(_registrationEndpoint.Host, "none")} endpoint_path={NormalizeLogValue(_registrationEndpoint.AbsolutePath, "none")} app_version={ResolveAppVersionForLog()}");
            using var request = new HttpRequestMessage(HttpMethod.Post, _registrationEndpoint)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token).ConfigureAwait(false);

            _logger.Info("Security", $"client registration response status={(int)response.StatusCode}");
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed or HttpStatusCode.ServiceUnavailable)
            {
                _logger.Info("Security", $"client registration unsupported status={(int)response.StatusCode}");
                return OptiClickClientRegistrationResult.Unsupported();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.Info("Security", $"client registration unavailable status={(int)response.StatusCode}");
                return OptiClickClientRegistrationResult.TransientFailure();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<OptiClickClientRegistrationResponse>(
                stream,
                SerializerOptions,
                timeoutCts.Token).ConfigureAwait(false);

            if (payload?.Ok != true
                || payload.SchemaVersion != 1
                || string.IsNullOrWhiteSpace(payload.ClientId)
                || string.IsNullOrWhiteSpace(payload.ClientSecret))
            {
                _logger.Info("Security", "client registration invalid_payload");
                return OptiClickClientRegistrationResult.TransientFailure();
            }

            _logger.Info(
                "Security",
                $"client registration success client_id_present=true created_at_present={FormatBool(payload.CreatedAtUtc != default)}");
            return OptiClickClientRegistrationResult.Success(
                new OptiClickClientCredential
                {
                    SchemaVersion = payload.SchemaVersion,
                    ClientId = payload.ClientId.Trim(),
                    ClientSecret = payload.ClientSecret.Trim(),
                    CreatedAtUtc = payload.CreatedAtUtc == default
                        ? DateTimeOffset.UtcNow
                        : payload.CreatedAtUtc
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Info("Security", $"client registration skipped type={ex.GetType().Name}");
            return OptiClickClientRegistrationResult.TransientFailure();
        }
    }

    private string BuildUserAgentValue()
    {
        var version = (_appVersionProvider() ?? "").Trim();
        return $"{UserAgentProduct}/{(string.IsNullOrWhiteSpace(version) ? "0.0.0" : version)}";
    }

    private string ResolveAppVersionForLog()
    {
        return NormalizeLogValue(_appVersionProvider(), "none");
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public interface IOptiClickClientCredentialProvider
{
    Task<OptiClickClientCredential?> GetCredentialAsync(CancellationToken cancellationToken = default);
}

public sealed class OptiClickClientCredentialProvider : IOptiClickClientCredentialProvider
{
    private static readonly TimeSpan RegistrationRetryBackoff = TimeSpan.FromSeconds(30);

    private readonly IOptiClickClientCredentialStore _store;
    private readonly IOptiClickClientRegistrationClient _registrationClient;
    private readonly Func<string?> _appVersionProvider;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private OptiClickClientCredential? _cachedCredential;
    private bool _storeLoaded;
    private bool _registrationUnsupported;
    private DateTimeOffset _nextRegistrationAttemptUtc;

    public OptiClickClientCredentialProvider(
        IOptiClickClientCredentialStore store,
        IOptiClickClientRegistrationClient registrationClient,
        Func<string?> appVersionProvider,
        IAppLogger? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _registrationClient = registrationClient ?? throw new ArgumentNullException(nameof(registrationClient));
        _appVersionProvider = appVersionProvider ?? (() => "");
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<OptiClickClientCredential?> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedCredential is not null)
        {
            _logger.Debug("Security", "client credential provider cache_hit=true");
            return _cachedCredential;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedCredential is not null)
            {
                _logger.Debug("Security", "client credential provider cache_hit=true scope=locked");
                return _cachedCredential;
            }

            if (!_storeLoaded)
            {
                _logger.Debug("Security", "client credential provider store_load start");
                _cachedCredential = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
                _storeLoaded = true;
                if (_cachedCredential is not null)
                {
                    _logger.Info("Security", "client credential provider store_hit=true");
                    return _cachedCredential;
                }

                _logger.Info("Security", "client credential provider store_hit=false");
            }

            if (_registrationUnsupported)
            {
                _logger.Info("Security", "client credential provider registration skipped reason=unsupported_cached");
                return null;
            }

            if (_nextRegistrationAttemptUtc > DateTimeOffset.UtcNow)
            {
                var remainingSeconds = Math.Max(0, (int)Math.Ceiling((_nextRegistrationAttemptUtc - DateTimeOffset.UtcNow).TotalSeconds));
                _logger.Info("Security", $"client credential provider registration skipped reason=backoff seconds_remaining={remainingSeconds}");
                return null;
            }

            _logger.Info(
                "Security",
                $"client credential provider registration start app_version={NormalizeLogValue(_appVersionProvider(), "none")}");
            var registrationResult = await _registrationClient.RegisterAsync(cancellationToken).ConfigureAwait(false);
            if (!registrationResult.IsSuccess || registrationResult.Credential is null)
            {
                if (registrationResult.IsUnsupported)
                {
                    _registrationUnsupported = true;
                    _logger.Info("Security", "client credential provider registration result=unsupported");
                }
                else
                {
                    _nextRegistrationAttemptUtc = DateTimeOffset.UtcNow.Add(RegistrationRetryBackoff);
                    _logger.Info(
                        "Security",
                        $"client credential provider registration result=transient backoff_seconds={(int)RegistrationRetryBackoff.TotalSeconds}");
                }

                return null;
            }

            var registered = registrationResult.Credential;
            await _store.SaveAsync(
                registered,
                _appVersionProvider() ?? "",
                cancellationToken).ConfigureAwait(false);
            _cachedCredential = registered;
            _logger.Info("Security", "client credential provider registration result=success saved=true");
            return _cachedCredential;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public interface IOptiClickApiSession
{
    string AppStartId { get; }
}

public sealed class OptiClickApiSession : IOptiClickApiSession
{
    public string AppStartId { get; } = Guid.NewGuid().ToString("N");
}

public sealed record OptiClickApiRequestContext
{
    public string AppVersion { get; init; } = "";
    public string ManifestVersion { get; init; } = "";
    public bool IncludeSignature { get; init; } = true;
    public string BundleTicket { get; init; } = "";
    public string DownloadTicket { get; init; } = "";
}

public interface IOptiClickApiTicketStore
{
    string BundleTicket { get; }

    void SetBundleTicket(string? value);

    void ClearBundleTicket();
}

public sealed class OptiClickApiTicketStore : IOptiClickApiTicketStore
{
    private readonly object _sync = new();
    private string _bundleTicket = "";

    public string BundleTicket
    {
        get
        {
            lock (_sync)
            {
                return _bundleTicket;
            }
        }
    }

    public void SetBundleTicket(string? value)
    {
        lock (_sync)
        {
            _bundleTicket = (value ?? "").Trim();
        }
    }

    public void ClearBundleTicket()
    {
        SetBundleTicket("");
    }
}

public static class OptiClickBase64Url
{
    public static string Encode(byte[] value)
    {
        return Convert.ToBase64String(value ?? [])
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static byte[] Decode(string value)
    {
        var normalized = (value ?? "").Trim()
            .Replace('-', '+')
            .Replace('_', '/');

        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => "",
            _ => throw new FormatException("Invalid base64url length.")
        };

        return Convert.FromBase64String(normalized);
    }
}

public sealed class OptiClickRequestCanonicalizer
{
    public const string EmptyBodySha256Base64Url = "47DEQpj8HBSa-_TImW-5JCeuQeRkm5NMpJWZG3hSuFU";

    public string BuildCanonicalRequest(
        HttpRequestMessage request,
        string timestamp,
        string nonce,
        string appVersionFallback,
        string manifestVersionFallback = "")
    {
        ArgumentNullException.ThrowIfNull(request);
        var uri = request.RequestUri ?? new Uri("https://invalid.local/");
        var appVersion = ReadFirstQueryValue(uri, "app_version");
        if (string.IsNullOrWhiteSpace(appVersion))
        {
            appVersion = (appVersionFallback ?? "").Trim();
        }

        var manifestVersion = ReadFirstQueryValue(uri, "manifest_version");
        if (string.IsNullOrWhiteSpace(manifestVersion))
        {
            manifestVersion = (manifestVersionFallback ?? "").Trim();
        }

        return string.Join(
            "\n",
            [
                (request.Method?.Method ?? "GET").ToUpperInvariant(),
                uri.AbsolutePath,
                Sha256Base64Url(BuildCanonicalQueryString(uri)),
                EmptyBodySha256Base64Url,
                (timestamp ?? "").Trim(),
                (nonce ?? "").Trim(),
                appVersion,
                manifestVersion
            ]);
    }

    public string BuildCanonicalQueryString(Uri uri)
    {
        var entries = ParseQueryEntries(uri)
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Value, StringComparer.Ordinal)
            .Select(static entry => $"{EncodeURIComponent(entry.Key)}={EncodeURIComponent(entry.Value)}");
        return string.Join("&", entries);
    }

    public static string Sha256Base64Url(string value)
    {
        return OptiClickBase64Url.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")));
    }

    public static string EncodeURIComponent(string value)
    {
        return Uri.EscapeDataString(value ?? "")
            .Replace("%21", "!", StringComparison.OrdinalIgnoreCase)
            .Replace("%27", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("%28", "(", StringComparison.OrdinalIgnoreCase)
            .Replace("%29", ")", StringComparison.OrdinalIgnoreCase)
            .Replace("%2A", "*", StringComparison.OrdinalIgnoreCase);
    }

    public static string ReadFirstQueryValue(Uri uri, string key)
    {
        var target = key ?? "";
        foreach (var entry in ParseQueryEntries(uri))
        {
            if (string.Equals(entry.Key, target, StringComparison.Ordinal))
            {
                return entry.Value;
            }
        }

        return "";
    }

    private static IEnumerable<(string Key, string Value)> ParseQueryEntries(Uri uri)
    {
        var query = (uri?.Query ?? "").Trim();
        if (query.StartsWith("?", StringComparison.Ordinal))
        {
            query = query[1..];
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            yield break;
        }

        foreach (var segment in query.Split('&', StringSplitOptions.None))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            var equalsIndex = segment.IndexOf('=');
            var rawKey = equalsIndex < 0 ? segment : segment[..equalsIndex];
            var rawValue = equalsIndex < 0 ? "" : segment[(equalsIndex + 1)..];
            yield return (DecodeQueryComponent(rawKey), DecodeQueryComponent(rawValue));
        }
    }

    private static string DecodeQueryComponent(string value)
    {
        return Uri.UnescapeDataString((value ?? "").Replace("+", " ", StringComparison.Ordinal));
    }
}

public interface IOptiClickHmacSigner
{
    string SignWithBase64UrlSecret(string clientSecret, string text);
}

public sealed class OptiClickHmacSigner : IOptiClickHmacSigner
{
    public string SignWithBase64UrlSecret(string clientSecret, string text)
    {
        var key = OptiClickBase64Url.Decode(clientSecret);
        using var hmac = new HMACSHA256(key);
        return OptiClickBase64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")));
    }
}

public interface IOptiClickApiRequestAuthenticator
{
    Task ApplyAsync(
        HttpRequestMessage request,
        OptiClickApiRequestContext context,
        CancellationToken cancellationToken = default);
}

public sealed class OptiClickApiRequestAuthenticator : IOptiClickApiRequestAuthenticator
{
    private readonly IOptiClickClientCredentialProvider _credentialProvider;
    private readonly IOptiClickHmacSigner _hmacSigner;
    private readonly OptiClickRequestCanonicalizer _canonicalizer;
    private readonly IOptiClickApiSession _session;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<string> _nonceFactory;
    private readonly IAppLogger _logger;

    public OptiClickApiRequestAuthenticator(
        IOptiClickClientCredentialProvider credentialProvider,
        IOptiClickHmacSigner hmacSigner,
        OptiClickRequestCanonicalizer canonicalizer,
        IOptiClickApiSession session,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? nonceFactory = null,
        IAppLogger? logger = null)
    {
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _hmacSigner = hmacSigner ?? throw new ArgumentNullException(nameof(hmacSigner));
        _canonicalizer = canonicalizer ?? throw new ArgumentNullException(nameof(canonicalizer));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _nonceFactory = nonceFactory ?? CreateNonce;
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task ApplyAsync(
        HttpRequestMessage request,
        OptiClickApiRequestContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var safeContext = context ?? new OptiClickApiRequestContext();
        var appVersion = (safeContext.AppVersion ?? "").Trim();
        var method = NormalizeLogValue(request.Method?.Method, "none");
        var path = NormalizeLogValue(request.RequestUri?.AbsolutePath, "none");

        _logger.Debug(
            "Security",
            $"api auth apply start method={method} path={path} app_version={NormalizeLogValue(appVersion, "none")} include_signature={FormatBool(safeContext.IncludeSignature)} manifest_version_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.ManifestVersion))} bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.BundleTicket))} download_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.DownloadTicket))}");

        SetHeader(request, OptiClickProtocolHeaders.Protocol, "2");
        SetHeader(request, OptiClickProtocolHeaders.AppVersion, appVersion);
        SetHeader(request, OptiClickProtocolHeaders.AppStartId, _session.AppStartId);
        SetOptionalHeader(request, OptiClickProtocolHeaders.BundleTicket, safeContext.BundleTicket);
        SetOptionalHeader(request, OptiClickProtocolHeaders.DownloadTicket, safeContext.DownloadTicket);
        _logger.Debug(
            "Security",
            $"api auth base headers applied method={method} path={path} protocol2=true app_start_id_present={FormatBool(!string.IsNullOrWhiteSpace(_session.AppStartId))}");

        if (!safeContext.IncludeSignature)
        {
            _logger.Info("Security", $"api auth signature skipped reason=include_signature_false method={method} path={path}");
            return;
        }

        try
        {
            var credential = await _credentialProvider.GetCredentialAsync(cancellationToken).ConfigureAwait(false);
            if (credential is null
                || string.IsNullOrWhiteSpace(credential.ClientId)
                || string.IsNullOrWhiteSpace(credential.ClientSecret))
            {
                _logger.Info("Security", $"api auth signature skipped reason=credential_missing method={method} path={path}");
                return;
            }

            var timestamp = _utcNow().ToUnixTimeSeconds().ToString();
            var nonce = _nonceFactory();
            var canonical = _canonicalizer.BuildCanonicalRequest(
                request,
                timestamp,
                nonce,
                appVersion,
                safeContext.ManifestVersion);
            var signature = _hmacSigner.SignWithBase64UrlSecret(credential.ClientSecret, canonical);
            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.Info("Security", $"api auth signature skipped reason=signature_empty method={method} path={path}");
                return;
            }

            SetHeader(request, OptiClickProtocolHeaders.ClientId, credential.ClientId);
            SetHeader(request, OptiClickProtocolHeaders.Timestamp, timestamp);
            SetHeader(request, OptiClickProtocolHeaders.Nonce, nonce);
            SetHeader(request, OptiClickProtocolHeaders.Signature, signature);
            _logger.Info(
                "Security",
                $"api auth signature applied method={method} path={path} client_id_present=true timestamp_present=true manifest_version_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.ManifestVersion))} bundle_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.BundleTicket))} download_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(safeContext.DownloadTicket))}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RemoveHeader(request, OptiClickProtocolHeaders.ClientId);
            RemoveHeader(request, OptiClickProtocolHeaders.Timestamp);
            RemoveHeader(request, OptiClickProtocolHeaders.Nonce);
            RemoveHeader(request, OptiClickProtocolHeaders.Signature);
            _logger.Warning("Security", $"api auth signature skipped reason=exception method={method} path={path} type={ex.GetType().Name}");
        }
    }

    private static void SetOptionalHeader(HttpRequestMessage request, string name, string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            RemoveHeader(request, name);
            return;
        }

        SetHeader(request, name, normalized);
    }

    private static void SetHeader(HttpRequestMessage request, string name, string value)
    {
        RemoveHeader(request, name);
        request.Headers.TryAddWithoutValidation(name, value ?? "");
    }

    private static void RemoveHeader(HttpRequestMessage request, string name)
    {
        if (request.Headers.Contains(name))
        {
            request.Headers.Remove(name);
        }
    }

    private static string CreateNonce()
    {
        return OptiClickBase64Url.Encode(RandomNumberGenerator.GetBytes(18));
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed record ExtraBundleResourcePath
{
    public string Bundle { get; init; } = "";
    public string Version { get; init; } = "";
    public string Filename { get; init; } = "";
}

public interface IRemoteDownloadTicketClient
{
    bool CanRequestTicketForSource(string sourceUrl);

    Task<string> TryGetDownloadTicketAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteDownloadTicketClient : IRemoteDownloadTicketClient
{
    private const string UserAgentProduct = "OptiClick";
    private readonly HttpClient _httpClient;
    private readonly Uri? _downloadTicketEndpoint;
    private readonly IOptiClickApiRequestAuthenticator? _authenticator;
    private readonly Func<string?> _appVersionProvider;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _timeout;

    public RemoteDownloadTicketClient(
        HttpClient httpClient,
        Uri? downloadTicketEndpoint,
        IOptiClickApiRequestAuthenticator? authenticator,
        Func<string?> appVersionProvider,
        IAppLogger? logger = null,
        TimeSpan? timeout = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _downloadTicketEndpoint = downloadTicketEndpoint;
        _authenticator = authenticator;
        _appVersionProvider = appVersionProvider ?? (() => "");
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<string> TryGetDownloadTicketAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _downloadTicketEndpoint;
        if (endpoint is null)
        {
            _logger.Info("Security", "download ticket skipped reason=endpoint_missing");
            return "";
        }

        if (!TryParseExtraBundleResourceUrl(sourceUrl, out var resource))
        {
            _logger.Info("Security", "download ticket skipped reason=invalid_resource_url");
            return "";
        }

        if (!HasSameOrigin(sourceUrl, endpoint))
        {
            _logger.Info(
                "Security",
                $"download ticket skipped reason=origin_mismatch source_host={ResolveHostForLog(sourceUrl)} endpoint_host={NormalizeLogValue(endpoint.Host, "none")}");
            return "";
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            _logger.Info(
                "Security",
                $"download ticket request start bundle={NormalizeLogValue(resource.Bundle, "none")} version={NormalizeLogValue(resource.Version, "none")} filename={NormalizeLogValue(resource.Filename, "none")} app_version={ResolveAppVersionForLog()} authenticator_configured={FormatBool(_authenticator is not null)}");
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildTicketRequestUri(endpoint, resource));
            request.Headers.UserAgent.ParseAdd(BuildUserAgentValue());
            request.Headers.Accept.ParseAdd("application/json");
            if (_authenticator is not null)
            {
                await _authenticator.ApplyAsync(
                    request,
                    new OptiClickApiRequestContext
                    {
                        AppVersion = (_appVersionProvider() ?? "").Trim()
                    },
                    timeoutCts.Token).ConfigureAwait(false);
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token).ConfigureAwait(false);

            _logger.Info("Security", $"download ticket response status={(int)response.StatusCode}");
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable)
            {
                _logger.Info("Security", $"download ticket unsupported status={(int)response.StatusCode}");
                return "";
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.Info("Security", $"download ticket unavailable status={(int)response.StatusCode}");
                return "";
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("ok", out var ok)
                || ok.ValueKind != JsonValueKind.True
                || !document.RootElement.TryGetProperty("download_ticket", out var ticketElement)
                || ticketElement.ValueKind != JsonValueKind.String)
            {
                _logger.Info("Security", "download ticket invalid_payload");
                return "";
            }

            var ticket = ticketElement.GetString()?.Trim() ?? "";
            _logger.Info("Security", $"download ticket success ticket_present={FormatBool(!string.IsNullOrWhiteSpace(ticket))}");
            return ticket;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Info("Security", $"download ticket skipped type={ex.GetType().Name}");
            return "";
        }
    }

    public bool CanRequestTicketForSource(string sourceUrl)
    {
        return _downloadTicketEndpoint is not null
            && TryParseExtraBundleResourceUrl(sourceUrl, out _)
            && HasSameOrigin(sourceUrl, _downloadTicketEndpoint);
    }

    public static bool TryParseExtraBundleResourceUrl(string sourceUrl, out ExtraBundleResourcePath resource)
    {
        resource = new ExtraBundleResourcePath();
        if (!Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (segments.Length != 6
            || !string.Equals(segments[0], "v1", StringComparison.Ordinal)
            || !string.Equals(segments[1], "resources", StringComparison.Ordinal)
            || !string.Equals(segments[2], "extra_bundle", StringComparison.Ordinal))
        {
            return false;
        }

        var bundle = segments[3].Trim();
        var version = segments[4].Trim();
        var filename = segments[5].Trim();
        if (!IsSafeExtraBundleToken(bundle)
            || !IsSafeExtraBundleToken(version)
            || !IsSafeExtraBundleToken(filename))
        {
            return false;
        }

        resource = new ExtraBundleResourcePath
        {
            Bundle = bundle,
            Version = version,
            Filename = filename
        };
        return true;
    }

    private static bool HasSameOrigin(string sourceUrl, Uri endpoint)
    {
        if (!Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var source))
        {
            return false;
        }

        return string.Equals(source.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(source.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
            && source.Port == endpoint.Port;
    }

    private static Uri BuildTicketRequestUri(Uri endpoint, ExtraBundleResourcePath resource)
    {
        var builder = new UriBuilder(endpoint)
        {
            Query = string.Join(
                "&",
                [
                    $"bundle={Uri.EscapeDataString(resource.Bundle)}",
                    $"version={Uri.EscapeDataString(resource.Version)}",
                    $"filename={Uri.EscapeDataString(resource.Filename)}"
                ])
        };
        return builder.Uri;
    }

    private static bool IsSafeExtraBundleToken(string value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal)
            || normalized.Contains('/', StringComparison.Ordinal)
            || normalized.Contains('\0', StringComparison.Ordinal))
        {
            return false;
        }

        return normalized.All(static ch =>
            ch is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.'
                or '_'
                or '-');
    }

    private string BuildUserAgentValue()
    {
        var version = (_appVersionProvider() ?? "").Trim();
        return $"{UserAgentProduct}/{(string.IsNullOrWhiteSpace(version) ? "0.0.0" : version)}";
    }

    private string ResolveAppVersionForLog()
    {
        return NormalizeLogValue(_appVersionProvider(), "none");
    }

    private static string ResolveHostForLog(string sourceUrl)
    {
        return Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var uri)
            ? NormalizeLogValue(uri.Host, "none")
            : "invalid";
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public interface IArchiveDownloadRequestPreparer
{
    Task PrepareAsync(
        HttpRequestMessage request,
        string sourceUrl,
        CancellationToken cancellationToken = default);
}

public sealed class OptiClickArchiveDownloadRequestPreparer : IArchiveDownloadRequestPreparer
{
    private readonly IRemoteDownloadTicketClient _downloadTicketClient;
    private readonly IOptiClickApiRequestAuthenticator _authenticator;
    private readonly Func<string?> _appVersionProvider;
    private readonly IAppLogger _logger;

    public OptiClickArchiveDownloadRequestPreparer(
        IRemoteDownloadTicketClient downloadTicketClient,
        IOptiClickApiRequestAuthenticator authenticator,
        Func<string?> appVersionProvider,
        IAppLogger? logger = null)
    {
        _downloadTicketClient = downloadTicketClient ?? throw new ArgumentNullException(nameof(downloadTicketClient));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _appVersionProvider = appVersionProvider ?? (() => "");
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task PrepareAsync(
        HttpRequestMessage request,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_downloadTicketClient.CanRequestTicketForSource(sourceUrl))
        {
            _logger.Info("Security", $"archive download prepare skipped reason=ticket_not_available source_host={ResolveHostForLog(sourceUrl)}");
            return;
        }

        _logger.Info(
            "Security",
            $"archive download prepare start source_host={ResolveHostForLog(sourceUrl)} app_version={NormalizeLogValue(_appVersionProvider(), "none")}");
        var downloadTicket = await _downloadTicketClient.TryGetDownloadTicketAsync(
            sourceUrl,
            cancellationToken).ConfigureAwait(false);
        _logger.Info("Security", $"archive download prepare ticket_result download_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(downloadTicket))}");

        await _authenticator.ApplyAsync(
            request,
            new OptiClickApiRequestContext
            {
                AppVersion = (_appVersionProvider() ?? "").Trim(),
                DownloadTicket = downloadTicket
            },
            cancellationToken).ConfigureAwait(false);
        _logger.Info(
            "Security",
            $"archive download prepare complete download_ticket_present={FormatBool(!string.IsNullOrWhiteSpace(downloadTicket))}");
    }

    private static string ResolveHostForLog(string sourceUrl)
    {
        return Uri.TryCreate((sourceUrl ?? "").Trim(), UriKind.Absolute, out var uri)
            ? NormalizeLogValue(uri.Host, "none")
            : "invalid";
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

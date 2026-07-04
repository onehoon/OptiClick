using System.Net.Http;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Logging;
using OptiClick.Wpf.Services;

namespace OptiClick.Wpf.Shell.RemoteV2;

public interface IAuthV2SessionClient
{
    Task<AuthV2SessionStartResult> StartSessionAsync(
        RuntimeContext runtimeContext,
        string appStartId,
        GpuInfo? selectedGpu = null,
        CancellationToken cancellationToken = default);
}

public interface IDataV2Client
{
    Task<DataV2RuntimeDataResult> FetchRuntimeDataAsync(
        string runtimeToken,
        string appStartId,
        CancellationToken cancellationToken = default);

    Task<DataV2GpuBundleResult> FetchGpuBundleAsync(
        string gpuBundleToken,
        string appStartId,
        CancellationToken cancellationToken = default);
}

public sealed class AuthV2SessionClient : IAuthV2SessionClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly IAuthV2CredentialStore _credentialStore;
    private readonly IAppVersionProvider _appVersionProvider;
    private readonly IAppLogger _logger;

    public AuthV2SessionClient(
        string baseUrl,
        HttpClient httpClient,
        IAuthV2CredentialStore credentialStore,
        IAppVersionProvider appVersionProvider,
        IAppLogger? logger = null)
    {
        _baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _appVersionProvider = appVersionProvider ?? throw new ArgumentNullException(nameof(appVersionProvider));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<AuthV2SessionStartResult> StartSessionAsync(
        RuntimeContext runtimeContext,
        string appStartId,
        GpuInfo? selectedGpu = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return Failure("auth_v2_endpoint_missing");
        }

        var payload = CreatePayload(runtimeContext, appStartId, selectedGpu);
        if (payload.Gpus.Count == 0)
        {
            return Failure("auth_v2_gpu_candidates_missing");
        }

        var rawBody = JsonSerializer.Serialize(payload, SerializerOptions);
        var credential = _credentialStore.Load();
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("/v2/auth/session/start"));
        request.Content = new StringContent(rawBody, Encoding.UTF8, "application/json");
        if (credential?.IsUsable == true)
        {
            try
            {
                AddSignedHeaders(request, credential, payload, rawBody);
            }
            catch (Exception ex)
            {
                _logger.Warning("remote-v2", $"auth v2 credential signing failed; falling back to bootstrap type={ex.GetType().Name}");
                _credentialStore.Clear();
            }
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = ParseSessionStartResponse(content, response.IsSuccessStatusCode, response.StatusCode);
            if (credential?.IsUsable == true && IsCredentialRejected(result.ErrorCode))
            {
                _logger.Warning("remote-v2", $"auth v2 credential rejected code={NormalizeLogValue(result.ErrorCode, "unknown")}; retrying bootstrap");
                _credentialStore.Clear();
                return await StartSessionAsync(runtimeContext, appStartId, selectedGpu, cancellationToken).ConfigureAwait(false);
            }

            if (result.CredentialIssued && !string.IsNullOrWhiteSpace(result.ClientSecret))
            {
                _credentialStore.Save(new AuthV2Credential
                {
                    ClientId = result.ClientId,
                    ClientSecret = result.ClientSecret
                });
            }

            if (result.IsResolved)
            {
                _logger.Info("remote-v2", "auth v2 session resolved");
            }
            else
            {
                _logger.Warning("remote-v2", $"auth v2 session not ready status={NormalizeLogValue(result.Status, "unknown")} code={NormalizeLogValue(result.ErrorCode, "none")}");
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure("auth_v2_canceled");
        }
        catch (Exception ex)
        {
            _logger.Error("remote-v2", "auth v2 session request failed", ex);
            return Failure("auth_v2_request_failed");
        }
    }

    private AuthV2SessionStartRequest CreatePayload(
        RuntimeContext runtimeContext,
        string appStartId,
        GpuInfo? selectedGpu)
    {
        var safeContext = runtimeContext ?? new RuntimeContext();
        var candidates = AuthV2GpuCandidateFilter.FilterUploadCandidates(safeContext.Gpus)
            .Select(gpu => new AuthV2GpuPayload
            {
                Name = gpu.Name,
                VendorHint = AuthV2GpuCandidateFilter.NormalizeVendor(gpu.Vendor, gpu.Name),
                IsPrimary = gpu.IsPrimary,
                Source = NormalizeSource(safeContext.HardwareDetection.GpuInfoSource)
            })
            .ToArray();

        var filteredSelected = selectedGpu is null
            ? safeContext.SelectedGpu
            : selectedGpu;
        var selectedPayload = CreateSelectedGpuPayload(filteredSelected, candidates);

        return new AuthV2SessionStartRequest
        {
            AppVersion = _appVersionProvider.GetCurrentVersion(),
            AppStartId = (appStartId ?? "").Trim(),
            Device = new AuthV2DevicePayload
            {
                Manufacturer = AuthV2GpuCandidateFilter.NormalizeWhitespace(safeContext.Device?.Manufacturer),
                Model = AuthV2GpuCandidateFilter.NormalizeWhitespace(safeContext.Device?.Model)
            },
            Gpus = candidates,
            SelectedGpu = selectedPayload,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nonce = Guid.NewGuid().ToString("N")
        };
    }

    private static AuthV2SelectedGpuPayload? CreateSelectedGpuPayload(
        GpuInfo? selectedGpu,
        IReadOnlyList<AuthV2GpuPayload> candidates)
    {
        if (selectedGpu is null)
        {
            return null;
        }

        var name = AuthV2GpuCandidateFilter.NormalizeWhitespace(selectedGpu.Name);
        var vendor = AuthV2GpuCandidateFilter.NormalizeVendor(selectedGpu.Vendor, name);
        if (!AuthV2GpuCandidateFilter.IsUploadSupported(vendor, name))
        {
            return null;
        }

        var candidateExists = candidates.Any(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.VendorHint, vendor, StringComparison.OrdinalIgnoreCase));
        if (!candidateExists)
        {
            return null;
        }

        return new AuthV2SelectedGpuPayload
        {
            Name = name,
            VendorHint = vendor
        };
    }

    private void AddSignedHeaders(
        HttpRequestMessage request,
        AuthV2Credential credential,
        AuthV2SessionStartRequest payload,
        string rawBody)
    {
        var path = request.RequestUri?.AbsolutePath ?? "/v2/auth/session/start";
        var signature = AuthV2RequestSigner.CreateSignature(
            "POST",
            path,
            credential.ClientId,
            payload.AppStartId,
            payload.Timestamp,
            payload.Nonce,
            rawBody,
            credential.ClientSecret);
        request.Headers.TryAddWithoutValidation("X-OptiClick-Client-Id", credential.ClientId);
        request.Headers.TryAddWithoutValidation("X-OptiClick-App-Start-Id", payload.AppStartId);
        request.Headers.TryAddWithoutValidation("X-OptiClick-Timestamp", payload.Timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation("X-OptiClick-Nonce", payload.Nonce);
        request.Headers.TryAddWithoutValidation("X-OptiClick-Signature", signature);
    }

    private AuthV2SessionStartResult ParseSessionStartResponse(string content, bool httpSuccess, System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Failure($"auth_v2_http_{(int)statusCode}");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var status = ReadString(root, "status");
            var sessionReady = ReadBool(root, "session_ready");
            var runtimeToken = ReadString(root, "runtime_token");
            var resolvedGpu = ReadResolvedGpu(root, "resolve_v2");
            return new AuthV2SessionStartResult
            {
                IsHttpSuccess = httpSuccess,
                ErrorCode = httpSuccess
                    ? ReadString(root, "error")
                    : NormalizeLogValue(ReadString(root, "error"), $"auth_v2_http_{(int)statusCode}"),
                Status = status,
                SessionReady = sessionReady,
                RuntimeToken = runtimeToken,
                RuntimeTokenExpiresIn = ReadInt(root, "runtime_token_expires_in"),
                CredentialIssued = ReadBool(root, "credential_issued"),
                ClientId = ReadString(root, "client_id"),
                ClientSecret = ReadString(root, "client_secret"),
                ResolvedGpu = resolvedGpu,
                Candidates = ReadCandidates(root)
            };
        }
        catch (JsonException)
        {
            return Failure("auth_v2_invalid_json");
        }
    }

    private static AuthV2ResolvedGpu ReadResolvedGpu(JsonElement root, string resolveProperty)
    {
        if (!root.TryGetProperty(resolveProperty, out var resolve) || resolve.ValueKind != JsonValueKind.Object)
        {
            return new AuthV2ResolvedGpu();
        }

        var manifestVersion = ReadString(resolve, "manifest_version");
        if (string.IsNullOrWhiteSpace(manifestVersion) && resolve.TryGetProperty("manifest_version", out var manifestElement))
        {
            manifestVersion = manifestElement.ToString();
        }

        if (!resolve.TryGetProperty("selected_gpu", out var gpu) || gpu.ValueKind != JsonValueKind.Object)
        {
            return new AuthV2ResolvedGpu();
        }

        return ReadResolvedGpuElement(gpu, manifestVersion);
    }

    private static IReadOnlyList<AuthV2ResolvedGpu> ReadCandidates(JsonElement root)
    {
        if (!root.TryGetProperty("resolve_v2", out var resolve)
            || resolve.ValueKind != JsonValueKind.Object
            || !resolve.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var manifestVersion = resolve.TryGetProperty("manifest_version", out var manifestElement)
            ? manifestElement.ToString()
            : "";
        return candidates.EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.Object)
            .Select(item => ReadResolvedGpuElement(item, manifestVersion))
            .ToArray();
    }

    private static AuthV2ResolvedGpu ReadResolvedGpuElement(JsonElement gpu, string manifestVersion)
    {
        return new AuthV2ResolvedGpu
        {
            CandidateId = ReadString(gpu, "candidate_id"),
            SourceIndex = ReadInt(gpu, "source_index"),
            Name = ReadString(gpu, "name"),
            Vendor = ReadString(gpu, "vendor"),
            BundleKey = ReadString(gpu, "bundle_key"),
            GpuGroup = ReadString(gpu, "gpu_group"),
            GpuRaw = ReadString(gpu, "gpu_raw"),
            GpuRawNormalized = ReadString(gpu, "gpu_raw_normalized"),
            ManifestVersion = manifestVersion
        };
    }

    private Uri BuildUri(string path)
    {
        return new Uri($"{_baseUrl}{path}", UriKind.Absolute);
    }

    private static string NormalizeSource(string? value)
    {
        var source = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(source) ? "runtime" : source;
    }

    private static AuthV2SessionStartResult Failure(string errorCode)
    {
        return new AuthV2SessionStartResult
        {
            IsHttpSuccess = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }

    private static bool IsCredentialRejected(string errorCode)
    {
        return string.Equals(errorCode, "client_not_found", StringComparison.OrdinalIgnoreCase)
               || string.Equals(errorCode, "invalid_signature", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString()?.Trim() ?? "",
            JsonValueKind.Number => property.ToString(),
            _ => ""
        };
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return int.TryParse(ReadString(element, propertyName), out var parsed) ? parsed : 0;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.True;
    }
}

public sealed class DataV2Client : IDataV2Client
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;

    public DataV2Client(
        string baseUrl,
        HttpClient httpClient,
        IAppLogger? logger = null)
    {
        _baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<DataV2RuntimeDataResult> FetchRuntimeDataAsync(
        string runtimeToken,
        string appStartId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return new DataV2RuntimeDataResult { ErrorCode = "data_v2_endpoint_missing" };
        }

        using var request = CreateBearerRequest("/v2/runtime-data", runtimeToken, appStartId);
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseRuntimeDataResponse(content, response.IsSuccessStatusCode, response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DataV2RuntimeDataResult { ErrorCode = "data_v2_runtime_canceled" };
        }
        catch (Exception ex)
        {
            _logger.Error("remote-v2", "data v2 runtime-data request failed", ex);
            return new DataV2RuntimeDataResult { ErrorCode = "data_v2_runtime_request_failed" };
        }
    }

    public async Task<DataV2GpuBundleResult> FetchGpuBundleAsync(
        string gpuBundleToken,
        string appStartId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return new DataV2GpuBundleResult { ErrorCode = "data_v2_endpoint_missing" };
        }

        using var request = CreateBearerRequest("/v2/gpu-bundle", gpuBundleToken, appStartId);
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseGpuBundleResponse(content, response.IsSuccessStatusCode, response.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DataV2GpuBundleResult { ErrorCode = "data_v2_gpu_bundle_canceled" };
        }
        catch (Exception ex)
        {
            _logger.Error("remote-v2", "data v2 gpu-bundle request failed", ex);
            return new DataV2GpuBundleResult { ErrorCode = "data_v2_gpu_bundle_request_failed" };
        }
    }

    private HttpRequestMessage CreateBearerRequest(string path, string token, string appStartId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{_baseUrl}{path}", UriKind.Absolute));
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {(token ?? "").Trim()}");
        request.Headers.TryAddWithoutValidation("X-OptiClick-App-Start-Id", (appStartId ?? "").Trim());
        return request;
    }

    private static DataV2RuntimeDataResult ParseRuntimeDataResponse(
        string content,
        bool httpSuccess,
        System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new DataV2RuntimeDataResult { ErrorCode = $"data_v2_http_{(int)statusCode}" };
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (!httpSuccess)
            {
                return new DataV2RuntimeDataResult { ErrorCode = ReadString(root, "error") };
            }

            if (!root.TryGetProperty("runtime_data", out var runtimeData))
            {
                return new DataV2RuntimeDataResult { ErrorCode = "data_v2_runtime_missing" };
            }

            return new DataV2RuntimeDataResult
            {
                IsSuccess = true,
                RuntimeDataJson = runtimeData.GetRawText(),
                GpuBundleToken = ReadString(root, "gpu_bundle_token"),
                GpuBundleTokenExpiresIn = ReadInt(root, "gpu_bundle_token_expires_in")
            };
        }
        catch (JsonException)
        {
            return new DataV2RuntimeDataResult { ErrorCode = "data_v2_invalid_json" };
        }
    }

    private static DataV2GpuBundleResult ParseGpuBundleResponse(
        string content,
        bool httpSuccess,
        System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new DataV2GpuBundleResult { ErrorCode = $"data_v2_http_{(int)statusCode}" };
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (!httpSuccess)
            {
                return new DataV2GpuBundleResult { ErrorCode = ReadString(root, "error") };
            }

            if (!root.TryGetProperty("gpu_bundle", out var gpuBundle))
            {
                return new DataV2GpuBundleResult { ErrorCode = "data_v2_gpu_bundle_missing" };
            }

            return new DataV2GpuBundleResult
            {
                IsSuccess = true,
                GpuBundleJson = gpuBundle.GetRawText()
            };
        }
        catch (JsonException)
        {
            return new DataV2GpuBundleResult { ErrorCode = "data_v2_invalid_json" };
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return "";
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString()?.Trim() ?? "",
            JsonValueKind.Number => property.ToString(),
            _ => ""
        };
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return int.TryParse(ReadString(element, propertyName), out var parsed) ? parsed : 0;
    }
}

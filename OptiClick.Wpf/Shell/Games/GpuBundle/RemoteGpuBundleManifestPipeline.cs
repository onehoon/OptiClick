using System.Net.Http;
using System.Text;
using System.Text.Json;
using OptiClick.Core.Runtime;
using OptiClick.Wpf.Logging;
using OptiClick.Wpf.Shell.Games.Support;

namespace OptiClick.Wpf.Shell.Games.GpuBundle;

public sealed class RemoteGpuBundleManifest
{
    public IReadOnlyList<RemoteGpuBundleManifestRule> Rules { get; init; } = [];
    public RemoteGpuBundleManifestFallback? Fallback { get; init; }
    public string ManifestVersion { get; init; } = "";
}

public sealed class RemoteGpuBundleManifestRule
{
    public bool Enabled { get; init; } = true;
    public string Vendor { get; init; } = "";
    public string MatchMode { get; init; } = "";
    public string MatchValue { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public int Priority { get; init; } = 100;
    public int SourceIndex { get; init; }
}

public sealed class RemoteGpuBundleManifestFallback
{
    public bool Enabled { get; init; } = true;
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
}

public sealed class RemoteGpuBundleManifestParseResult
{
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundleManifest Manifest { get; init; } = new();

    public static RemoteGpuBundleManifestParseResult Success(RemoteGpuBundleManifest manifest)
    {
        return new RemoteGpuBundleManifestParseResult
        {
            IsSuccess = true,
            Manifest = manifest ?? new RemoteGpuBundleManifest()
        };
    }

    public static RemoteGpuBundleManifestParseResult Failure(string errorCode)
    {
        return new RemoteGpuBundleManifestParseResult
        {
            IsSuccess = false,
            ErrorCode = (errorCode ?? "").Trim(),
            Manifest = new RemoteGpuBundleManifest()
        };
    }
}

public interface IRemoteGpuBundleManifestParser
{
    RemoteGpuBundleManifestParseResult Parse(string json);
}

public sealed class RemoteGpuBundleManifestParser : IRemoteGpuBundleManifestParser
{
    public RemoteGpuBundleManifestParseResult Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RemoteGpuBundleManifestParseResult.Failure("empty_input");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return RemoteGpuBundleManifestParseResult.Failure("invalid_json");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return RemoteGpuBundleManifestParseResult.Failure("payload_not_object");
            }

            var rules = ParseRules(root);
            var fallback = ParseFallback(root);
            var manifestVersion = ReadString(root, "manifest_version");

            return RemoteGpuBundleManifestParseResult.Success(new RemoteGpuBundleManifest
            {
                Rules = rules,
                Fallback = fallback,
                ManifestVersion = manifestVersion
            });
        }
    }

    private static IReadOnlyList<RemoteGpuBundleManifestRule> ParseRules(JsonElement root)
    {
        if (!root.TryGetProperty("rules", out var rulesElement)
            || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rules = new List<RemoteGpuBundleManifestRule>();
        var index = 0;
        foreach (var element in rulesElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            rules.Add(new RemoteGpuBundleManifestRule
            {
                Enabled = ReadBool(element, "enabled", true),
                Vendor = NormalizeVendor(ReadString(element, "vendor")),
                MatchMode = ReadString(element, "match_mode").ToLowerInvariant(),
                MatchValue = ReadString(element, "match_value"),
                BundleKey = ReadString(element, "bundle_key"),
                GpuGroup = ReadString(element, "gpu_group").ToLowerInvariant(),
                Priority = ReadInt(element, "priority", 100),
                SourceIndex = index
            });
            index++;
        }

        return rules;
    }

    private static RemoteGpuBundleManifestFallback? ParseFallback(JsonElement root)
    {
        if (!root.TryGetProperty("fallback", out var fallbackElement)
            || fallbackElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new RemoteGpuBundleManifestFallback
        {
            Enabled = ReadBool(fallbackElement, "enabled", true),
            BundleKey = ReadString(fallbackElement, "bundle_key"),
            GpuGroup = ReadString(fallbackElement, "gpu_group").ToLowerInvariant()
        };
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return SupportFlagParser.Parse(ParseJsonValue(property), emptyDefault: defaultValue, unknownDefault: defaultValue, nativeXefgMeansFalse: false);
    }

    private static int ReadInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse((property.GetString() ?? "").Trim(), out var parsed))
        {
            return parsed;
        }

        return defaultValue;
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
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => property.ToString().Trim()
        };
    }

    private static object ParseJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.Null => "",
            _ => value.ToString()
        };
    }

    private static string NormalizeVendor(string vendor)
    {
        var text = (vendor ?? "").Trim().ToLowerInvariant();
        if (text.Contains("nvidia", StringComparison.Ordinal))
        {
            return "nvidia";
        }

        if (text.Contains("intel", StringComparison.Ordinal))
        {
            return "intel";
        }

        if (text.Contains("amd", StringComparison.Ordinal) || text.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }
}

public sealed class RemoteGpuBundleManifestFetchResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string ErrorCode { get; init; } = "";
    public RemoteGpuBundleManifest Manifest { get; init; } = new();

    public static RemoteGpuBundleManifestFetchResult Success(RemoteGpuBundleManifest manifest)
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = true,
            Manifest = manifest ?? new RemoteGpuBundleManifest()
        };
    }

    public static RemoteGpuBundleManifestFetchResult Skipped()
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = false,
            IsSkipped = true
        };
    }

    public static RemoteGpuBundleManifestFetchResult Failure(string errorCode)
    {
        return new RemoteGpuBundleManifestFetchResult
        {
            IsSuccess = false,
            IsSkipped = false,
            ErrorCode = (errorCode ?? "").Trim()
        };
    }
}

public sealed class GpuBundleManifestFetchRequest
{
    public string Vendor { get; init; } = "";
    public string GpuRaw { get; init; } = "";
    public string DeviceManufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public string RequestSource { get; init; } = "app";
    public string AppVersion { get; init; } = "";
}

public interface IGpuBundleManifestRequestUriBuilder
{
    Uri? Build(string endpoint, GpuBundleManifestFetchRequest request);
}

public sealed class GpuBundleManifestRequestUriBuilder : IGpuBundleManifestRequestUriBuilder
{
    public Uri? Build(string endpoint, GpuBundleManifestFetchRequest request)
    {
        // Python parity requires manifest requests without query parameters.
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var baseUri))
        {
            return null;
        }
        return baseUri;
    }
}

public interface IRemoteGpuBundleManifestClient
{
    Task<RemoteGpuBundleManifestFetchResult> FetchAsync(
        string endpoint,
        GpuBundleManifestFetchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RemoteGpuBundleManifestClient : IRemoteGpuBundleManifestClient
{
    private readonly HttpClient _httpClient;
    private readonly IGpuBundleManifestRequestUriBuilder _requestUriBuilder;
    private readonly IRemoteGpuBundleManifestParser _parser;
    private readonly TimeSpan _timeout;
    private readonly IAppLogger _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public RemoteGpuBundleManifestClient(
        HttpClient httpClient,
        IGpuBundleManifestRequestUriBuilder requestUriBuilder,
        IRemoteGpuBundleManifestParser parser,
        IAppLogger? logger = null,
        TimeSpan? timeout = null,
        int maxAttempts = 3,
        TimeSpan? retryDelay = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestUriBuilder = requestUriBuilder ?? throw new ArgumentNullException(nameof(requestUriBuilder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? NullAppLogger.Instance;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _maxAttempts = Math.Max(1, maxAttempts);
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(300);
    }

    public async Task<RemoteGpuBundleManifestFetchResult> FetchAsync(
        string endpoint,
        GpuBundleManifestFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEndpoint = (endpoint ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalizedEndpoint))
        {
            _logger.Warning("remote", "gpu-bundle-manifest skipped code=manifest_endpoint_missing");
            return RemoteGpuBundleManifestFetchResult.Skipped();
        }

        var requestUri = _requestUriBuilder.Build(normalizedEndpoint, request);
        if (requestUri is null)
        {
            _logger.Error("remote", "gpu-bundle-manifest failed code=invalid_manifest_endpoint");
            return RemoteGpuBundleManifestFetchResult.Failure("invalid_manifest_endpoint");
        }

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            _logger.Info("remote", "gpu-bundle-manifest request url_set=true");
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using var response = await _httpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorCode = $"manifest_http_{(int)response.StatusCode}";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("remote", $"gpu-bundle-manifest failed code={errorCode}");
                    return RemoteGpuBundleManifestFetchResult.Failure(errorCode);
                }

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                if (string.IsNullOrWhiteSpace(content))
                {
                    const string errorCode = "empty_manifest_response";
                    if (ShouldRetry(errorCode, attempt)
                        && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                    {
                        continue;
                    }

                    _logger.Error("remote", "gpu-bundle-manifest failed code=empty_manifest_response");
                    return RemoteGpuBundleManifestFetchResult.Failure(errorCode);
                }

                var parsed = _parser.Parse(content);
                if (!parsed.IsSuccess)
                {
                    _logger.Error("remote", $"gpu-bundle-manifest failed code={NormalizeLogValue(parsed.ErrorCode, "manifest_parse_failed")}");
                    return RemoteGpuBundleManifestFetchResult.Failure(parsed.ErrorCode);
                }

                _logger.Info("remote", $"gpu-bundle-manifest success rules={parsed.Manifest.Rules.Count} manifest_version={NormalizeLogValue(parsed.Manifest.ManifestVersion, "none")}");

                return RemoteGpuBundleManifestFetchResult.Success(parsed.Manifest);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("remote", "gpu-bundle-manifest canceled code=manifest_canceled");
                return RemoteGpuBundleManifestFetchResult.Failure("manifest_canceled");
            }
            catch (OperationCanceledException)
            {
                const string errorCode = "manifest_timeout";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle-manifest failed code=manifest_timeout");
                return RemoteGpuBundleManifestFetchResult.Failure(errorCode);
            }
            catch (HttpRequestException ex)
            {
                const string errorCode = "manifest_request_failed";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                var status = ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "none";
                _logger.Error("remote", $"gpu-bundle-manifest failed code=manifest_request_failed status={status}", ex);
                return RemoteGpuBundleManifestFetchResult.Failure(errorCode);
            }
            catch (Exception ex)
            {
                const string errorCode = "manifest_unexpected_error";
                if (ShouldRetry(errorCode, attempt)
                    && await TryDelayBeforeRetryAsync(attempt, errorCode, cancellationToken))
                {
                    continue;
                }

                _logger.Error("remote", "gpu-bundle-manifest failed code=manifest_unexpected_error", ex);
                return RemoteGpuBundleManifestFetchResult.Failure(errorCode);
            }
        }

        return RemoteGpuBundleManifestFetchResult.Failure("manifest_request_failed");
    }

    private bool ShouldRetry(string errorCode, int attempt)
    {
        if (attempt >= _maxAttempts)
        {
            return false;
        }

        if (TryParseHttpStatusCode(errorCode, "manifest_http_", out var statusCode))
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        return string.Equals(errorCode, "manifest_timeout", StringComparison.Ordinal)
               || string.Equals(errorCode, "manifest_request_failed", StringComparison.Ordinal)
               || string.Equals(errorCode, "empty_manifest_response", StringComparison.Ordinal)
               || string.Equals(errorCode, "manifest_unexpected_error", StringComparison.Ordinal);
    }

    private static bool TryParseHttpStatusCode(string errorCode, string prefix, out int statusCode)
    {
        statusCode = 0;
        if (string.IsNullOrWhiteSpace(errorCode)
            || string.IsNullOrWhiteSpace(prefix)
            || !errorCode.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(errorCode[prefix.Length..], out statusCode);
    }

    private async Task<bool> TryDelayBeforeRetryAsync(
        int attempt,
        string errorCode,
        CancellationToken cancellationToken)
    {
        var nextAttempt = attempt + 1;
        _logger.Warning("remote", $"gpu-bundle-manifest retry scheduled attempt={nextAttempt} code={errorCode}");

        if (_retryDelay <= TimeSpan.Zero)
        {
            return true;
        }

        try
        {
            var delay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * attempt);
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}

public sealed class GpuBundleRuleMatchResult
{
    public bool IsMatched { get; init; }
    public bool IsUnsupported { get; init; }
    public string ErrorCode { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string BundleKey { get; init; } = "";
    public string GpuGroup { get; init; } = "";
    public string GpuRaw { get; init; } = "";
}

public interface IGpuBundleManifestRuleResolver
{
    GpuBundleRuleMatchResult Resolve(RemoteGpuBundleManifest manifest, RuntimeContext runtimeContext);
}

public sealed class GpuBundleManifestRuleResolver : IGpuBundleManifestRuleResolver
{
    public GpuBundleRuleMatchResult Resolve(RemoteGpuBundleManifest manifest, RuntimeContext runtimeContext)
    {
        if (manifest is null)
        {
            return new GpuBundleRuleMatchResult { ErrorCode = "manifest_missing" };
        }

        var selectedGpu = ResolveSelectedGpu(runtimeContext);
        if (selectedGpu is null)
        {
            return new GpuBundleRuleMatchResult
            {
                ErrorCode = HasMultipleGpuCandidates(runtimeContext?.Gpus)
                    ? "gpu_selection_pending"
                    : "gpu_not_found"
            };
        }

        var normalizedVendor = NormalizeVendor(selectedGpu.Vendor, selectedGpu.Name);
        var normalizedGpuRaw = NormalizeGpuMatchText(selectedGpu.Name);
        if (string.IsNullOrWhiteSpace(normalizedVendor) || string.IsNullOrWhiteSpace(normalizedGpuRaw))
        {
            return new GpuBundleRuleMatchResult { ErrorCode = "gpu_unsupported" };
        }

        var matches = new List<(int Priority, int MatchLengthNegative, int Index, RemoteGpuBundleManifestRule Rule)>();
        foreach (var rule in manifest.Rules)
        {
            if (rule is null || !rule.Enabled)
            {
                continue;
            }

            if (!string.Equals(rule.Vendor, normalizedVendor, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedMatchValue = NormalizeGpuMatchText(rule.MatchValue);
            if (string.IsNullOrWhiteSpace(normalizedMatchValue))
            {
                continue;
            }

            var matched = false;
            if (string.Equals(rule.MatchMode, "exact", StringComparison.OrdinalIgnoreCase))
            {
                matched = string.Equals(normalizedGpuRaw, normalizedMatchValue, StringComparison.Ordinal);
            }
            else if (string.Equals(rule.MatchMode, "contains", StringComparison.OrdinalIgnoreCase))
            {
                matched = normalizedGpuRaw.Contains(normalizedMatchValue, StringComparison.Ordinal);
            }

            if (!matched)
            {
                continue;
            }

            matches.Add((rule.Priority, -normalizedMatchValue.Length, rule.SourceIndex, rule));
        }

        if (matches.Count > 0)
        {
            var selected = matches
                .OrderBy(static item => item.Priority)
                .ThenBy(static item => item.MatchLengthNegative)
                .ThenBy(static item => item.Index)
                .First().Rule;

            return new GpuBundleRuleMatchResult
            {
                IsMatched = true,
                Vendor = normalizedVendor,
                BundleKey = (selected.BundleKey ?? "").Trim(),
                GpuGroup = (selected.GpuGroup ?? "").Trim().ToLowerInvariant(),
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        var fallback = manifest.Fallback;
        if (fallback is null || !fallback.Enabled)
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "bundle_rule_not_matched",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        var fallbackBundleKey = (fallback.BundleKey ?? "").Trim();
        var fallbackGroup = (fallback.GpuGroup ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fallbackBundleKey) || string.IsNullOrWhiteSpace(fallbackGroup))
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "fallback_invalid",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        if (string.Equals(fallbackBundleKey, "unknown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackGroup, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return new GpuBundleRuleMatchResult
            {
                IsUnsupported = true,
                ErrorCode = "bundle_rule_not_matched",
                Vendor = normalizedVendor,
                GpuRaw = NormalizeSpace(selectedGpu.Name)
            };
        }

        return new GpuBundleRuleMatchResult
        {
            IsMatched = true,
            Vendor = normalizedVendor,
            BundleKey = fallbackBundleKey,
            GpuGroup = fallbackGroup,
            GpuRaw = NormalizeSpace(selectedGpu.Name)
        };
    }

    private static GpuInfo? ResolveSelectedGpu(RuntimeContext runtimeContext)
    {
        if (runtimeContext?.SelectedGpu is not null)
        {
            return runtimeContext.SelectedGpu;
        }

        var gpus = BuildDistinctGpuCandidates(runtimeContext?.Gpus);
        return gpus.Count == 1 ? gpus[0] : null;
    }

    private static IReadOnlyList<GpuInfo> BuildDistinctGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        if (gpus is null || gpus.Count == 0)
        {
            return [];
        }

        var list = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var name = NormalizeSpace(gpu.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var vendor = NormalizeSpace(gpu.Vendor);
            var key = $"{vendor}|{name}";
            if (seen.Add(key))
            {
                list.Add(gpu);
            }
        }

        return list;
    }

    private static bool HasMultipleGpuCandidates(IReadOnlyList<GpuInfo>? gpus)
    {
        return BuildDistinctGpuCandidates(gpus).Count > 1;
    }

    private static string NormalizeVendor(string vendor, string gpuName)
    {
        var candidate = $"{vendor} {gpuName}".Trim().ToLowerInvariant();
        if (candidate.Contains("nvidia", StringComparison.Ordinal))
        {
            return "nvidia";
        }

        if (candidate.Contains("intel", StringComparison.Ordinal))
        {
            return "intel";
        }

        if (candidate.Contains("amd", StringComparison.Ordinal) || candidate.Contains("radeon", StringComparison.Ordinal))
        {
            return "amd";
        }

        return "";
    }

    private static string NormalizeSpace(string value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string NormalizeGpuMatchText(string value)
    {
        var text = NormalizeSpace(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = System.Text.RegularExpressions.Regex.Replace(text, @"\((?:tm|r)\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = text.Replace("\u2122", "").Replace("\u00AE", "");
        return NormalizeSpace(text).ToLowerInvariant();
    }
}



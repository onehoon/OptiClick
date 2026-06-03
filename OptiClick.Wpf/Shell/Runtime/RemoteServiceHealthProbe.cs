using System.Net.Http;
using System.Text.Json;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RemoteServiceHealthStatus
{
    public string Indicator { get; init; } = "unknown";
    public string Description { get; init; } = "";
    public string ErrorCode { get; init; } = "";
}

public sealed record RemoteServiceHealthSnapshot
{
    public RemoteServiceHealthStatus Cloudflare { get; init; } = new();
    public RemoteServiceHealthStatus GitHub { get; init; } = new();
}

public interface IRemoteServiceHealthProbe
{
    Task<RemoteServiceHealthSnapshot> ProbeAsync(CancellationToken cancellationToken = default);
}

public sealed class RemoteServiceHealthProbe : IRemoteServiceHealthProbe
{
    private static readonly Uri CloudflareStatusUri = new("https://www.cloudflarestatus.com/api/v2/summary.json");
    private static readonly Uri GitHubStatusUri = new("https://www.githubstatus.com/api/v2/summary.json");

    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;

    public RemoteServiceHealthProbe(
        HttpClient httpClient,
        TimeSpan? timeout = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _timeout = timeout ?? TimeSpan.FromSeconds(2);
    }

    public async Task<RemoteServiceHealthSnapshot> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var cloudflareTask = ProbeSingleAsync(CloudflareStatusUri, cancellationToken);
        var githubTask = ProbeSingleAsync(GitHubStatusUri, cancellationToken);
        await Task.WhenAll(cloudflareTask, githubTask);

        return new RemoteServiceHealthSnapshot
        {
            Cloudflare = cloudflareTask.Result,
            GitHub = githubTask.Result
        };
    }

    private async Task<RemoteServiceHealthStatus> ProbeSingleAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OptiClick");
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new RemoteServiceHealthStatus
                {
                    ErrorCode = $"http_{(int)response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new RemoteServiceHealthStatus
                {
                    ErrorCode = "empty_response"
                };
            }

            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new RemoteServiceHealthStatus
                {
                    ErrorCode = "payload_not_object"
                };
            }

            var status = document.RootElement.TryGetProperty("status", out var statusElement)
                && statusElement.ValueKind == JsonValueKind.Object
                ? statusElement
                : default;
            var indicator = ReadString(status, "indicator");
            var description = ReadString(status, "description");
            return new RemoteServiceHealthStatus
            {
                Indicator = string.IsNullOrWhiteSpace(indicator) ? "unknown" : indicator,
                Description = description
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new RemoteServiceHealthStatus
            {
                ErrorCode = "canceled"
            };
        }
        catch (OperationCanceledException)
        {
            return new RemoteServiceHealthStatus
            {
                ErrorCode = "timeout"
            };
        }
        catch (HttpRequestException)
        {
            return new RemoteServiceHealthStatus
            {
                ErrorCode = "request_failed"
            };
        }
        catch (JsonException)
        {
            return new RemoteServiceHealthStatus
            {
                ErrorCode = "invalid_json"
            };
        }
        catch
        {
            return new RemoteServiceHealthStatus
            {
                ErrorCode = "unexpected_error"
            };
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
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
}

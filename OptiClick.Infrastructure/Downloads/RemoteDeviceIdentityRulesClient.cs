using System.Net.Http;

namespace OptiClick.Infrastructure.Downloads;

public sealed class RemoteDeviceIdentityRulesClient : IRemoteDeviceIdentityRulesClient
{
    private readonly string _endpoint;
    private readonly bool _enabled;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _timeout;

    public RemoteDeviceIdentityRulesClient(
        string endpoint,
        bool enabled,
        HttpClient httpClient,
        TimeSpan? timeout = null)
    {
        _endpoint = (endpoint ?? "").Trim();
        _enabled = enabled;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<RemoteDeviceIdentityRulesFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            return RemoteDeviceIdentityRulesFetchResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(_endpoint))
        {
            return RemoteDeviceIdentityRulesFetchResult.Skipped();
        }

        if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var endpointUri))
        {
            return RemoteDeviceIdentityRulesFetchResult.Failure("invalid_endpoint");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpointUri);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return RemoteDeviceIdentityRulesFetchResult.Failure($"http_{(int)response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (string.IsNullOrWhiteSpace(content))
            {
                return RemoteDeviceIdentityRulesFetchResult.Failure("empty_response");
            }

            return RemoteDeviceIdentityRulesFetchResult.Success(content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RemoteDeviceIdentityRulesFetchResult.Failure("canceled");
        }
        catch (OperationCanceledException)
        {
            return RemoteDeviceIdentityRulesFetchResult.Failure("timeout");
        }
        catch (HttpRequestException)
        {
            return RemoteDeviceIdentityRulesFetchResult.Failure("request_failed");
        }
        catch
        {
            return RemoteDeviceIdentityRulesFetchResult.Failure("unexpected_error");
        }
    }
}

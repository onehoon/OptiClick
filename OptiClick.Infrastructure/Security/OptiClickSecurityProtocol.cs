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

public interface IOptiClickApiRequestAuthenticator
{
    Task ApplyAsync(
        HttpRequestMessage request,
        OptiClickApiRequestContext context,
        CancellationToken cancellationToken = default);
}

public interface IArchiveDownloadRequestPreparer
{
    Task PrepareAsync(
        HttpRequestMessage request,
        string sourceUrl,
        CancellationToken cancellationToken = default);
}

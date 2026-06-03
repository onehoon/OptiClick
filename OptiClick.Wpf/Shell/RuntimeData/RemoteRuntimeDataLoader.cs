using OptiClick.Infrastructure.Downloads;

namespace OptiClick.Wpf.Shell.RuntimeData;

public sealed class RemoteRuntimeDataLoader : IRemoteRuntimeDataLoader
{
    private readonly IRemoteRuntimeDataClient _client;
    private readonly IRemoteRuntimeDataParser _parser;

    public RemoteRuntimeDataLoader(
        IRemoteRuntimeDataClient client,
        IRemoteRuntimeDataParser parser)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public async Task<RemoteRuntimeDataLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fetchResult = await _client.FetchAsync(cancellationToken);
            if (fetchResult.IsSkipped)
            {
                return RemoteRuntimeDataLoadResult.Skipped();
            }

            if (!fetchResult.IsSuccess)
            {
                return RemoteRuntimeDataLoadResult.Failure(
                    fetchResult.ErrorCode,
                    fetchResult.ErrorMessage);
            }

            var parseResult = _parser.Parse(fetchResult.Content);
            if (!parseResult.IsSuccess)
            {
                return RemoteRuntimeDataLoadResult.Failure(
                    parseResult.ErrorCode,
                    parseResult.ErrorMessage);
            }

            return RemoteRuntimeDataLoadResult.Success(parseResult.RuntimeData);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return RemoteRuntimeDataLoadResult.Failure("canceled");
        }
        catch
        {
            return RemoteRuntimeDataLoadResult.Failure("unexpected_error");
        }
    }
}


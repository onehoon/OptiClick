namespace OptiClick.Wpf.Install.Archives;

public sealed record ArchiveProcessResult
{
    public bool IsSuccess { get; init; }
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = "";
    public string StandardError { get; init; } = "";

    public static ArchiveProcessResult FromInfrastructure(OptiClick.Infrastructure.Archives.ArchiveProcessResult result)
    {
        return new ArchiveProcessResult
        {
            IsSuccess = result.IsSuccess,
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError
        };
    }
}

public interface IArchiveProcessRunner
{
    Task<ArchiveProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class ArchiveProcessRunner : IArchiveProcessRunner
{
    private readonly OptiClick.Infrastructure.Archives.ArchiveProcessRunner _inner = new();

    public async Task<ArchiveProcessResult> RunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.RunAsync(fileName, arguments, timeout, cancellationToken);
        return ArchiveProcessResult.FromInfrastructure(result);
    }
}

namespace OptiClick.Wpf.Shell.Support;

public sealed record LogFolderLaunchResult
{
    public bool IsSuccess { get; init; }
    public string ErrorType { get; init; } = "";
}

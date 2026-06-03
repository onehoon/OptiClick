namespace OptiClick.Wpf.Install.Config;

public interface IProfilePathResolver
{
    string? Resolve(string targetPath, string configuredPath, bool requireExisting);
}

namespace OptiClick.Wpf.Shell.RuntimeData;

public interface IRemoteRuntimeDataParser
{
    RemoteRuntimeDataParseResult Parse(string json);
}

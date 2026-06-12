namespace OptiClick.Core.RuntimeData;

public interface IRemoteRuntimeDataParser
{
    RemoteRuntimeDataParseResult Parse(string json);
}

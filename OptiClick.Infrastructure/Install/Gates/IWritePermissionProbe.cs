namespace OptiClick.Infrastructure.Install.Gates;

public interface IWritePermissionProbe
{
    WritePermissionProbeResult Probe(string targetFolder);
}


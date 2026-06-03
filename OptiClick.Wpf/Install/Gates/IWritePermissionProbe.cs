namespace OptiClick.Wpf.Install.Gates;

public interface IWritePermissionProbe
{
    WritePermissionProbeResult Probe(string targetFolder);
}


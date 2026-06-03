namespace OptiClick.Wpf.Install.Gates;

public interface IInstallStartGateResolver
{
    InstallStartGateDecision Resolve(InstallStartGateInput input);
}


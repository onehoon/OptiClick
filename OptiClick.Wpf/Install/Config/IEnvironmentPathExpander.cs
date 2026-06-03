namespace OptiClick.Wpf.Install.Config;

public interface IEnvironmentPathExpander
{
    string ExpandEnvironmentVariables(string value);

    string ExpandUserHome(string value);

    string GetUserHomeDirectory();
}

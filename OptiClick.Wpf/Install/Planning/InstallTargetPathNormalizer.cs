namespace OptiClick.Wpf.Install.Planning;

public static class InstallTargetPathNormalizer
{
    private static readonly InstallTargetPathValidator Validator = new();

    public static string NormalizeTargetDirectory(string? candidatePath)
    {
        return Validator.NormalizeTargetDirectory(candidatePath);
    }
}

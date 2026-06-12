using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Config;

internal static class ConfigProfileApplyRunner
{
    public static ConfigProfileApplyResult Apply(
        IConfigProfileApplier applier,
        string targetPath,
        ConfigApplyProfileRows profileRows)
    {
        ArgumentNullException.ThrowIfNull(applier);
        var safeProfileRows = profileRows ?? ConfigApplyProfileRows.Empty;

        var applyContext = new ConfigProfileApplyContext
        {
            TargetPath = targetPath,
            ProfileRows = safeProfileRows
        };

        return applier.Apply(applyContext);
    }
}

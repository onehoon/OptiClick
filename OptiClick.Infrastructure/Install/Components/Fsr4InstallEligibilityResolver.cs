using OptiClick.Core.Install;

namespace OptiClick.Infrastructure.Install.Components;

public interface IFsr4InstallEligibilityResolver
{
    Fsr4InstallEligibility Resolve(Fsr4InstallEligibilityContext context);
}

public sealed class Fsr4InstallEligibilityResolver : IFsr4InstallEligibilityResolver
{
    private readonly CoreFsr4InstallPolicy _installPolicy;

    public Fsr4InstallEligibilityResolver()
    {
        _installPolicy = new CoreFsr4InstallPolicy();
    }

    public Fsr4InstallEligibility Resolve(Fsr4InstallEligibilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.UseFsr4)
        {
            return new Fsr4InstallEligibility
            {
                CanInstall = false,
                SkipReason = "fsr4_skipped_by_policy"
            };
        }

        if (!_installPolicy.ShouldInstall(context.UseFsr4, context.Fsr4Variant))
        {
            return new Fsr4InstallEligibility
            {
                CanInstall = false,
                SkipReason = "fsr4_variant_missing"
            };
        }

        return new Fsr4InstallEligibility
        {
            CanInstall = true
        };
    }
}

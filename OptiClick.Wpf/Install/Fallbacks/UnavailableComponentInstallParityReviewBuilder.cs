using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Fallbacks;

internal sealed class UnavailableComponentInstallParityReviewBuilder : IComponentInstallParityReviewBuilder
{
    public ComponentInstallParityReviewResult Build(ComponentInstallParityReviewInput input)
    {
        _ = input;
        return new ComponentInstallParityReviewResult
        {
            IsSuccess = true,
            Events = []
        };
    }
}

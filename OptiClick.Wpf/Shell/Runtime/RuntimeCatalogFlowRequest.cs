using OptiClick.Core.Runtime;
using OptiClick.Wpf.Localization;

namespace OptiClick.Wpf.Shell.Runtime;

public sealed record RuntimeCatalogFlowRequest
{
    public required RuntimeContext LatestRuntimeContext { get; init; }
    public required AppLanguage SelectedLanguage { get; init; }
    public required RuntimeCatalogFlowText Text { get; init; }
}

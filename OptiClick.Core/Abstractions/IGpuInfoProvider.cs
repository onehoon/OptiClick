using OptiClick.Core.Runtime;

namespace OptiClick.Core.Abstractions;

public interface IGpuInfoProvider
{
    IReadOnlyList<GpuInfo> GetGpus();
}

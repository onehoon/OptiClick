using OptiClick.Core.Runtime;

namespace OptiClick.Core.Abstractions;

public interface IRuntimeHardwareDetectionInfoProvider
{
    RuntimeHardwareDetectionInfo GetHardwareDetectionInfo();
}

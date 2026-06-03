using OptiClick.Core.Runtime;

namespace OptiClick.Core.Abstractions;

public interface IRuntimeContextProvider
{
    RuntimeContext GetRuntimeContext();
}

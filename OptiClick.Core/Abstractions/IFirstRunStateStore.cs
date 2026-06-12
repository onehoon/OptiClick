using OptiClick.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OptiClick.Core.Abstractions;

public interface IFirstRunStateStore
{
    Task<FirstRunState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(FirstRunState state, CancellationToken cancellationToken = default);
}

using OptiClick.Core.Runtime;

namespace OptiClick.Core.Abstractions;

public interface IAppLanguageProvider
{
    AppLanguage CurrentLanguage { get; }
}

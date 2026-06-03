using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public interface IWritableAppLanguageProvider : IAppLanguageProvider
{
    IReadOnlyList<AppLanguage> SupportedLanguages { get; }
    void SetLanguage(AppLanguage language);
}

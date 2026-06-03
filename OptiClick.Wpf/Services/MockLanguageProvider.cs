using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class MockLanguageProvider : IWritableAppLanguageProvider
{
    public IReadOnlyList<AppLanguage> SupportedLanguages { get; } = [AppLanguage.English, AppLanguage.Korean];

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = language;
    }
}

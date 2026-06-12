using OptiClick.Core.Models;

namespace OptiClick.Core.Abstractions;

public interface IAppUserSettingsStore
{
    AppUserSettings Load();
    void Save(AppUserSettings settings);
}

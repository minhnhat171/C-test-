using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface IAudioSettingsService
{
    AudioSettingsState Load(string preferencePrefix);

    void Save(string preferencePrefix, AudioSettingsState settings);
}

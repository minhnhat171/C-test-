using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class AudioSettingsService : IAudioSettingsService
{
    public AudioSettingsState Load(string preferencePrefix)
    {
        if (string.IsNullOrWhiteSpace(preferencePrefix))
        {
            return CreateDefault();
        }

        var language = Preferences.Default.Get($"{preferencePrefix}.language", "vi");
        var playbackMode = Preferences.Default.Get($"{preferencePrefix}.playbackMode", "tts");
        var autoNarrationEnabled = Preferences.Default.Get($"{preferencePrefix}.autoNarration", true);

        return new AudioSettingsState
        {
            LanguageCode = NormalizeLanguage(language),
            PlaybackMode = NormalizePlaybackMode(playbackMode),
            AutoNarrationEnabled = autoNarrationEnabled
        };
    }

    public void Save(string preferencePrefix, AudioSettingsState settings)
    {
        if (string.IsNullOrWhiteSpace(preferencePrefix))
        {
            return;
        }

        var normalized = settings ?? CreateDefault();

        Preferences.Default.Set($"{preferencePrefix}.language", NormalizeLanguage(normalized.LanguageCode));
        Preferences.Default.Set($"{preferencePrefix}.playbackMode", NormalizePlaybackMode(normalized.PlaybackMode));
        Preferences.Default.Set($"{preferencePrefix}.autoNarration", normalized.AutoNarrationEnabled);
    }

    private static AudioSettingsState CreateDefault()
    {
        return new AudioSettingsState();
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        return languageCode?.Trim().ToLowerInvariant() switch
        {
            "en" => "en",
            "zh" => "zh",
            "ko" => "ko",
            "fr" => "fr",
            _ => "vi"
        };
    }

    private static string NormalizePlaybackMode(string? playbackMode)
    {
        return playbackMode?.Trim().ToLowerInvariant() switch
        {
            "audio" => "audio",
            _ => "tts"
        };
    }
}

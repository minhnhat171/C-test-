namespace VinhKhanhGuide.App.Models;

public sealed class AudioSettingsState
{
    public string LanguageCode { get; init; } = "vi";
    public string PlaybackMode { get; init; } = "tts";
    public bool AutoNarrationEnabled { get; init; } = true;
}

public sealed class AudioSettingsOption
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FlagEmoji { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DisplayLabel => string.IsNullOrWhiteSpace(FlagEmoji) ? Label : $"{FlagEmoji} {Label}";

    public override string ToString() => DisplayLabel;
}

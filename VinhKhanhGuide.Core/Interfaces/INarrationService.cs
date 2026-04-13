using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface INarrationService
{
    Task NarrateAsync(
        POI poi,
        string? languageCode = null,
        string? playbackMode = null,
        CancellationToken cancellationToken = default);

    Task SpeakAsync(
        string text,
        string? languageCode = null,
        string? playbackMode = null,
        string? audioAssetPath = null,
        CancellationToken cancellationToken = default);

    Task StopAsync();
}

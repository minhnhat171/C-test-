using Microsoft.Maui.Media;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class NarrationService : INarrationService
{
    private readonly SemaphoreSlim _speakGate = new(1, 1);

    public async Task NarrateAsync(POI poi, CancellationToken cancellationToken = default)
    {
        await _speakGate.WaitAsync(cancellationToken);
        try
        {
            var text = string.IsNullOrWhiteSpace(poi.NarrationText)
                ? $"Bạn đang ở gần {poi.Name}."
                : poi.NarrationText;

            await TextToSpeech.Default.SpeakAsync(text, options: null, cancelToken: cancellationToken);
        }
        finally
        {
            _speakGate.Release();
        }
    }
}

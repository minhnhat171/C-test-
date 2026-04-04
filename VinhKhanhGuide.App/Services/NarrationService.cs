using Microsoft.Maui.Media;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class NarrationService : INarrationService
{
    private readonly object _speechSync = new();
    private CancellationTokenSource? _activeSpeechCts;

    public Task NarrateAsync(POI poi, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var text = poi.GetNarrationText(languageCode);

        return SpeakAsync(text, languageCode, cancellationToken);
    }

    public async Task SpeakAsync(string text, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CancellationTokenSource? previousSpeech;
        var currentSpeech = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_speechSync)
        {
            previousSpeech = _activeSpeechCts;
            _activeSpeechCts = currentSpeech;
        }

        previousSpeech?.Cancel();
        previousSpeech?.Dispose();

        try
        {
            var options = await CreateSpeechOptionsAsync(languageCode);
            await TextToSpeech.Default.SpeakAsync(text, options, currentSpeech.Token);
        }
        catch (OperationCanceledException) when (currentSpeech.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_speechSync)
            {
                if (ReferenceEquals(_activeSpeechCts, currentSpeech))
                {
                    _activeSpeechCts = null;
                }
            }

            currentSpeech.Dispose();
        }
    }

    public Task StopAsync()
    {
        CancellationTokenSource? activeSpeech;

        lock (_speechSync)
        {
            activeSpeech = _activeSpeechCts;
            _activeSpeechCts = null;
        }

        activeSpeech?.Cancel();
        return Task.CompletedTask;
    }

    private static async Task<SpeechOptions?> CreateSpeechOptionsAsync(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var locale = locales.FirstOrDefault(item =>
            item.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase) ||
            item.Language.StartsWith($"{languageCode}-", StringComparison.OrdinalIgnoreCase));

        return locale is null
            ? null
            : new SpeechOptions
            {
                Locale = locale,
                Pitch = 1.0f,
                Volume = 1.0f
            };
    }
}

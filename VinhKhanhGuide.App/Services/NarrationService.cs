using Microsoft.Maui.Media;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;
#if ANDROID
using Android.Media;
#endif

namespace VinhKhanhGuide.App.Services;

public class NarrationService : INarrationService
{
    private readonly object _speechSync = new();
    private CancellationTokenSource? _activeSpeechCts;
    private readonly object _audioSync = new();
    private readonly IAudioAssetCacheService _audioAssetCacheService;
#if ANDROID
    private MediaPlayer? _activeAudioPlayer;
#endif

    public NarrationService(IAudioAssetCacheService audioAssetCacheService)
    {
        _audioAssetCacheService = audioAssetCacheService;
    }

    public Task NarrateAsync(
        POI poi,
        string? languageCode = null,
        string? playbackMode = null,
        CancellationToken cancellationToken = default)
    {
        var text = poi.GetNarrationText(languageCode);

        return SpeakAsync(
            text,
            languageCode,
            playbackMode,
            poi.AudioAssetPath,
            cancellationToken);
    }

    public async Task SpeakAsync(
        string text,
        string? languageCode = null,
        string? playbackMode = null,
        string? audioAssetPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) &&
            !string.Equals(playbackMode, "audio", StringComparison.OrdinalIgnoreCase))
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
            if (string.Equals(playbackMode, "audio", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(audioAssetPath))
            {
                await PlayAudioAsync(audioAssetPath, currentSpeech.Token);
                return;
            }

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
#if ANDROID
        ReleaseActiveAudioPlayer();
#endif
        return Task.CompletedTask;
    }

    private static async Task<SpeechOptions?> CreateSpeechOptionsAsync(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var normalizedLanguage = languageCode.Trim().ToLowerInvariant();
        var locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();
        var locale = SelectPreferredLocale(locales, normalizedLanguage);

        return locale is null
            ? null
            : new SpeechOptions
            {
                Locale = locale,
                Pitch = 1.0f,
                Volume = 1.0f
            };
    }

    private static Locale? SelectPreferredLocale(
        IReadOnlyList<Locale> locales,
        string normalizedLanguage)
    {
        if (locales.Count == 0)
        {
            return null;
        }

        foreach (var preferredCode in GetPreferredLocaleCodes(normalizedLanguage))
        {
            var exactMatch = locales.FirstOrDefault(item =>
                string.Equals(item.Language, preferredCode, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
            {
                return exactMatch;
            }
        }

        return locales.FirstOrDefault(item =>
                   string.Equals(item.Language, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
               ?? locales.FirstOrDefault(item =>
                   item.Language.StartsWith($"{normalizedLanguage}-", StringComparison.OrdinalIgnoreCase))
               ?? locales.FirstOrDefault(item =>
                   item.Language.Contains(normalizedLanguage, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> GetPreferredLocaleCodes(string normalizedLanguage)
    {
        return normalizedLanguage switch
        {
            "en" => ["en-US", "en-GB", "en-AU", "en-CA"],
            "vi" => ["vi-VN"],
            "zh" => ["zh-CN", "zh-TW", "zh-HK"],
            "ko" => ["ko-KR"],
            "fr" => ["fr-FR", "fr-CA"],
            _ => [normalizedLanguage]
        };
    }

#if ANDROID
    private async Task PlayAudioAsync(string audioAssetPath, CancellationToken cancellationToken)
    {
        var resolvedSource = await _audioAssetCacheService.ResolveAsync(audioAssetPath, cancellationToken);
        var playbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var player = new MediaPlayer();
        var audioAttributesBuilder = new AudioAttributes.Builder()
            ?? throw new InvalidOperationException("Không thể khởi tạo audio attributes cho Android.");

        audioAttributesBuilder.SetContentType(AudioContentType.Speech);
        audioAttributesBuilder.SetUsage(AudioUsageKind.Media);

        var audioAttributes = audioAttributesBuilder.Build()
            ?? throw new InvalidOperationException("Không thể cấu hình audio attributes cho Android.");

        player.SetAudioAttributes(audioAttributes);

        player.Completion += (_, _) => playbackCompleted.TrySetResult(true);
        player.Error += (_, eventArgs) =>
        {
            eventArgs.Handled = true;
            playbackCompleted.TrySetException(new InvalidOperationException("Không thể phát file audio đã chọn."));
        };

        if (Uri.TryCreate(resolvedSource, UriKind.Absolute, out var sourceUri) &&
            (sourceUri.Scheme == Uri.UriSchemeHttp ||
             sourceUri.Scheme == Uri.UriSchemeHttps ||
             sourceUri.Scheme == Uri.UriSchemeFile))
        {
            player.SetDataSource(resolvedSource);
        }
        else
        {
            player.SetDataSource(resolvedSource);
        }

        ReplaceActiveAudioPlayer(player);

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (player.IsPlaying)
                {
                    player.Stop();
                }
            }
            catch
            {
            }

            playbackCompleted.TrySetCanceled(cancellationToken);
        });

        player.Prepare();
        player.Start();

        try
        {
            await playbackCompleted.Task;
        }
        finally
        {
            ReleaseAudioPlayer(player);
        }
    }

    private void ReplaceActiveAudioPlayer(MediaPlayer nextPlayer)
    {
        MediaPlayer? previousPlayer;

        lock (_audioSync)
        {
            previousPlayer = _activeAudioPlayer;
            _activeAudioPlayer = nextPlayer;
        }

        if (previousPlayer is not null && !ReferenceEquals(previousPlayer, nextPlayer))
        {
            ReleaseAudioPlayer(previousPlayer);
        }
    }

    private void ReleaseActiveAudioPlayer()
    {
        MediaPlayer? activePlayer;

        lock (_audioSync)
        {
            activePlayer = _activeAudioPlayer;
            _activeAudioPlayer = null;
        }

        ReleaseAudioPlayer(activePlayer);
    }

    private void ReleaseAudioPlayer(MediaPlayer? player)
    {
        if (player is null)
        {
            return;
        }

        lock (_audioSync)
        {
            if (ReferenceEquals(_activeAudioPlayer, player))
            {
                _activeAudioPlayer = null;
            }
        }

        try
        {
            if (player.IsPlaying)
            {
                player.Stop();
            }
        }
        catch
        {
        }

        player.Reset();
        player.Release();
        player.Dispose();
    }
#endif
}

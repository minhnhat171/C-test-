using Microsoft.Maui.Media;
using System.Linq;
using System.Threading.Tasks;

public static class TTSService
{
    public static async Task Speak(string text, string lang)
    {
        var locales = await TextToSpeech.GetLocalesAsync();

        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(lang));

        var options = new SpeechOptions
        {
            Locale = locale,
            Pitch = 1.0f,
            Volume = 1.0f
        };

        await TextToSpeech.SpeakAsync(text, options);
    }
}
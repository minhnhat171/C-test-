using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public sealed class TtsTranslationService
{
    public IReadOnlyDictionary<string, string> GenerateDemoScripts(
        PoiDto poi,
        IEnumerable<string> languageCodes)
    {
        var sourceText = string.IsNullOrWhiteSpace(poi.NarrationText)
            ? poi.Description
            : poi.NarrationText;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var languageCode in languageCodes
                     .Select(NormalizeLanguageCode)
                     .Where(code => !string.IsNullOrWhiteSpace(code))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            result[languageCode] = BuildDemoScript(languageCode, poi, sourceText);
        }

        return result;
    }

    private static string BuildDemoScript(string languageCode, PoiDto poi, string sourceText)
    {
        var title = string.IsNullOrWhiteSpace(poi.Name) ? poi.Code : poi.Name;
        var summary = string.IsNullOrWhiteSpace(sourceText)
            ? poi.Description
            : sourceText;

        return languageCode switch
        {
            "zh" => $"[ZH] Narration for {title}. Source Vietnamese content: {summary}",
            "ja" => $"[JA] Narration for {title}. Source Vietnamese content: {summary}",
            "de" => $"[DE] Narration for {title}. Source Vietnamese content: {summary}",
            _ => $"[EN] Narration for {title}. Source Vietnamese content: {summary}"
        };
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return languageCode?.Trim().ToLowerInvariant() switch
        {
            "zh-cn" => "zh",
            "zh-tw" => "zh",
            "ja-jp" => "ja",
            "de-de" => "de",
            "en-us" => "en",
            "en-gb" => "en",
            "zh" => "zh",
            "ja" => "ja",
            "de" => "de",
            "en" => "en",
            _ => string.Empty
        };
    }
}

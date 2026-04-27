using System.Text;

namespace VKFoodAPI.Services;

internal static class LegacyTextRepair
{
    private static readonly string[] MojibakeMarkers =
    [
        "Ã",
        "Â",
        "Ä",
        "Å",
        "Æ",
        "áº",
        "á»",
        "â€",
        "Ä‘",
        "Æ°"
    ];

    private static readonly string[] QuestionCorruptionTokens =
    [
        "B?n",
        "?ang",
        "? g?n",
        "qu?n",
        "V?nh",
        "Kh?nh",
        "Th? Gi?i",
        "N??ng",
        "L?u",
        "S??n",
        "Kh?ch",
        "?m th?c",
        "??"
    ];

    public static string Clean(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || !LooksLikeMojibake(trimmed))
        {
            return trimmed;
        }

        try
        {
            var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(trimmed)).Trim();
            return IsBetterCandidate(trimmed, repaired) ? repaired : trimmed;
        }
        catch
        {
            return trimmed;
        }
    }

    public static bool NeedsSeedFallback(string? value)
    {
        var cleaned = Clean(value);
        return string.IsNullOrWhiteSpace(cleaned) ||
               LooksLikeMojibake(cleaned) ||
               LooksLikeQuestionCorruption(cleaned);
    }

    public static bool LooksLikeQuestionCorruption(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed) ||
            !trimmed.Contains('?', StringComparison.Ordinal) ||
            Uri.IsWellFormedUriString(trimmed, UriKind.Absolute) ||
            trimmed.Contains('@', StringComparison.Ordinal))
        {
            return false;
        }

        return QuestionCorruptionTokens.Any(token =>
                   trimmed.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
               trimmed.Count(character => character == '?') >= 2;
    }

    private static bool LooksLikeMojibake(string value)
    {
        return MojibakeMarkers.Any(marker => value.Contains(marker, StringComparison.Ordinal));
    }

    private static bool IsBetterCandidate(string original, string repaired)
    {
        if (string.IsNullOrWhiteSpace(repaired))
        {
            return false;
        }

        var originalScore = CountSuspiciousCharacters(original);
        var repairedScore = CountSuspiciousCharacters(repaired);
        if (repairedScore != originalScore)
        {
            return repairedScore < originalScore;
        }

        return CountReadableCharacters(repaired) >= CountReadableCharacters(original);
    }

    private static int CountSuspiciousCharacters(string value)
    {
        var score = value.Count(character => character == '?') * 4 +
                    value.Count(character => character == '\uFFFD') * 8;

        foreach (var marker in MojibakeMarkers)
        {
            score += CountOccurrences(value, marker) * 6;
        }

        return score;
    }

    private static int CountReadableCharacters(string value)
    {
        return value.Count(character => char.IsLetterOrDigit(character));
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var startIndex = 0;

        while (startIndex < value.Length)
        {
            var index = value.IndexOf(token, startIndex, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            count++;
            startIndex = index + token.Length;
        }

        return count;
    }
}

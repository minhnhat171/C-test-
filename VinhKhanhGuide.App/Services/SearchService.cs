using System.Globalization;
using System.Text;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class SearchService : ISearchService
{
    private readonly IPoiRepository _poiRepository;

    public SearchService(IPoiRepository poiRepository)
    {
        _poiRepository = poiRepository;
    }

    public PoiSearchResult Search(string? keyword)
    {
        var trimmedKeyword = keyword?.Trim() ?? string.Empty;
        var results = _poiRepository.SearchCachedPois(trimmedKeyword);

        return new PoiSearchResult
        {
            Keyword = trimmedKeyword,
            Results = results
        };
    }

    public IReadOnlyList<SearchSuggestionItem> BuildSuggestions(
        string? keyword,
        IReadOnlyList<string> recentSearches,
        int maxSuggestions,
        int maxPinnedRecentSuggestions)
    {
        var trimmedKeyword = keyword?.Trim() ?? string.Empty;
        var normalizedKeyword = NormalizeForSearch(trimmedKeyword);
        var suggestions = new List<SearchSuggestionItem>();
        var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddSuggestion(string text, string supportingText, bool isRecent)
        {
            if (string.IsNullOrWhiteSpace(text) || !seenTexts.Add(text))
            {
                return;
            }

            suggestions.Add(new SearchSuggestionItem
            {
                Text = text,
                SupportingText = supportingText,
                IsRecent = isRecent
            });
        }

        if (recentSearches.Count > 0)
        {
            IEnumerable<string> recentItems;

            if (string.IsNullOrWhiteSpace(trimmedKeyword))
            {
                recentItems = recentSearches.Take(maxPinnedRecentSuggestions);
            }
            else
            {
                var matchedRecentItems = recentSearches
                    .Where(item => ContainsNormalized(item, normalizedKeyword))
                    .Take(maxPinnedRecentSuggestions)
                    .ToList();

                recentItems = matchedRecentItems.Count > 0
                    ? matchedRecentItems
                    : recentSearches.Take(maxPinnedRecentSuggestions);
            }

            foreach (var recent in recentItems)
            {
                AddSuggestion(recent, "Tìm gần đây", true);

                if (suggestions.Count >= maxSuggestions)
                {
                    return suggestions;
                }
            }
        }

        var matchedPois = string.IsNullOrWhiteSpace(trimmedKeyword)
            ? _poiRepository.GetCachedPois()
            : Search(trimmedKeyword).Results;

        foreach (var poi in matchedPois)
        {
            AddSuggestion(poi.Name, BuildSupportingText(poi, normalizedKeyword), false);

            if (suggestions.Count >= maxSuggestions)
            {
                break;
            }
        }

        return suggestions;
    }

    private static string BuildSupportingText(POI poi, string normalizedKeyword)
    {
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return poi.Address;
        }

        if (ContainsNormalized(poi.SpecialDish, normalizedKeyword))
        {
            return $"Món nổi bật: {poi.SpecialDish}";
        }

        if (ContainsNormalized(poi.Description, normalizedKeyword))
        {
            return poi.Description;
        }

        if (ContainsNormalized(poi.Code, normalizedKeyword))
        {
            return $"Mã quán: {poi.Code}";
        }

        return poi.Address;
    }

    private static bool ContainsNormalized(string? source, string normalizedKeyword)
    {
        return NormalizeForSearch(source).Contains(normalizedKeyword, StringComparison.Ordinal);
    }

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface ISearchService
{
    PoiSearchResult Search(string? keyword);

    IReadOnlyList<SearchSuggestionItem> BuildSuggestions(
        string? keyword,
        IReadOnlyList<string> recentSearches,
        int maxSuggestions,
        int maxPinnedRecentSuggestions);
}

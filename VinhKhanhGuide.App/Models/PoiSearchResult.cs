using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Models;

public sealed class PoiSearchResult
{
    public string Keyword { get; init; } = string.Empty;

    public IReadOnlyList<POI> Results { get; init; } = Array.Empty<POI>();

    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);

    public bool HasResults => Results.Count > 0;

    public string EmptyStateMessage => HasKeyword
        ? $"Không tìm thấy quán phù hợp với từ khóa \"{Keyword}\"."
        : string.Empty;
}

using System.Globalization;
using System.Text;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Mappings;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class PoiRepository : IPoiRepository
{
    private readonly IPoiProvider _poiProvider;
    private readonly object _syncRoot = new();
    private IReadOnlyList<POI> _cachedPois = Array.Empty<POI>();

    public PoiRepository(IPoiProvider poiProvider)
    {
        _poiProvider = poiProvider;
    }

    public async Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        var pois = await _poiProvider.GetPoisAsync(cancellationToken);
        StoreSnapshot(pois);
        return ClonePois(pois);
    }

    public async Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        if (poiId == Guid.Empty)
        {
            return null;
        }

        var cachedPoi = GetCachedPois().FirstOrDefault(item => item.Id == poiId);
        if (cachedPoi is not null)
        {
            return ClonePoi(cachedPoi);
        }

        var poi = await _poiProvider.GetPoiByIdAsync(poiId, cancellationToken);
        if (poi is null)
        {
            return null;
        }

        MergePoiIntoSnapshot(poi);
        return ClonePoi(poi);
    }

    public IReadOnlyList<POI> GetCachedPois()
    {
        lock (_syncRoot)
        {
            return ClonePois(_cachedPois);
        }
    }

    public IReadOnlyList<POI> SearchCachedPois(string? keyword)
    {
        var normalizedKeyword = NormalizeForSearch(keyword);
        var cachedPois = GetCachedPois();

        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return cachedPois;
        }

        return cachedPois
            .Select(poi => new
            {
                Poi = poi,
                Score = CalculateSearchScore(poi, normalizedKeyword)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Poi.Name)
            .Select(item => item.Poi)
            .ToList();
    }

    public void StoreSnapshot(IEnumerable<POI> pois)
    {
        lock (_syncRoot)
        {
            _cachedPois = ClonePois(pois);
        }
    }

    private void MergePoiIntoSnapshot(POI poi)
    {
        lock (_syncRoot)
        {
            var nextSnapshot = _cachedPois.ToList();
            var existingIndex = nextSnapshot.FindIndex(item => item.Id == poi.Id);

            if (existingIndex >= 0)
            {
                nextSnapshot[existingIndex] = ClonePoi(poi);
            }
            else
            {
                nextSnapshot.Add(ClonePoi(poi));
            }

            _cachedPois = nextSnapshot;
        }
    }

    private static int CalculateSearchScore(POI poi, string normalizedKeyword)
    {
        var score = 0;

        score = Math.Max(score, ScoreField(poi.Name, normalizedKeyword, exactScore: 1200, prefixScore: 950, containsScore: 800));
        score = Math.Max(score, ScoreField(poi.Code, normalizedKeyword, exactScore: 1000, prefixScore: 850, containsScore: 700));
        score = Math.Max(score, ScoreField(poi.SpecialDish, normalizedKeyword, exactScore: 760, prefixScore: 680, containsScore: 620));
        score = Math.Max(score, ScoreField(poi.Description, normalizedKeyword, exactScore: 560, prefixScore: 520, containsScore: 460));
        score = Math.Max(score, ScoreField(poi.Address, normalizedKeyword, exactScore: 430, prefixScore: 390, containsScore: 340));
        score = Math.Max(score, ScoreField(poi.Category, normalizedKeyword, exactScore: 320, prefixScore: 280, containsScore: 240));

        return score;
    }

    private static int ScoreField(
        string? source,
        string normalizedKeyword,
        int exactScore,
        int prefixScore,
        int containsScore)
    {
        var normalizedSource = NormalizeForSearch(source);
        if (string.IsNullOrWhiteSpace(normalizedSource))
        {
            return 0;
        }

        if (string.Equals(normalizedSource, normalizedKeyword, StringComparison.Ordinal))
        {
            return exactScore;
        }

        if (normalizedSource.StartsWith(normalizedKeyword, StringComparison.Ordinal))
        {
            return prefixScore;
        }

        return normalizedSource.Contains(normalizedKeyword, StringComparison.Ordinal)
            ? containsScore
            : 0;
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

    private static IReadOnlyList<POI> ClonePois(IEnumerable<POI> pois)
    {
        return pois.Select(ClonePoi).ToList();
    }

    private static POI ClonePoi(POI poi)
    {
        return poi.ToDto().ToDomain();
    }
}

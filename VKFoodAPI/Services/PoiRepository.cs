using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;

namespace VKFoodAPI.Services;

public class PoiRepository
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private List<PoiDto> _pois;

    public PoiRepository(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "pois.json");
        _pois = LoadPois();
    }

    public IReadOnlyList<PoiDto> GetAll()
    {
        lock (_syncRoot)
        {
            return _pois
                .Select(dto => dto.Clone())
                .ToList();
        }
    }

    public PoiDto? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            return _pois.FirstOrDefault(x => x.Id == id)?.Clone();
        }
    }

    public PoiDto Create(PoiDto dto)
    {
        lock (_syncRoot)
        {
            var created = Normalize(dto);
            created.Id = created.Id == Guid.Empty ? Guid.NewGuid() : created.Id;

            _pois.Add(created);
            SaveUnsafe();

            return created.Clone();
        }
    }

    public bool Update(Guid id, PoiDto dto)
    {
        lock (_syncRoot)
        {
            var index = _pois.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(dto);
            updated.Id = id;

            _pois[index] = updated;
            SaveUnsafe();

            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var removed = _pois.RemoveAll(x => x.Id == id);
            if (removed == 0)
            {
                return false;
            }

            SaveUnsafe();
            return true;
        }
    }

    public void ApplyPublishedAudioGuides(IEnumerable<AudioGuideDto> guides)
    {
        ArgumentNullException.ThrowIfNull(guides);

        lock (_syncRoot)
        {
            var groupedGuides = guides
                .Where(item => item.IsPublished && item.PoiId != Guid.Empty)
                .GroupBy(item => item.PoiId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(item => item.Clone())
                        .ToList());

            var hasChanges = false;

            for (var index = 0; index < _pois.Count; index++)
            {
                var poi = _pois[index];
                var updatedPoi = poi.Clone();
                updatedPoi.AudioAssetPath = string.Empty;

                if (!groupedGuides.TryGetValue(poi.Id, out var poiGuides) || poiGuides.Count == 0)
                {
                    var normalizedWithoutAudio = Normalize(updatedPoi);
                    if (!AreEquivalent(poi, normalizedWithoutAudio))
                    {
                        _pois[index] = normalizedWithoutAudio;
                        hasChanges = true;
                    }

                    continue;
                }

                foreach (var guide in poiGuides
                    .Where(item => string.Equals(item.SourceType, "tts", StringComparison.OrdinalIgnoreCase) &&
                                   !string.IsNullOrWhiteSpace(item.Script))
                    .GroupBy(item => NormalizeLanguageCode(item.LanguageCode), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group
                        .OrderByDescending(item => item.UpdatedAtUtc)
                        .First()))
                {
                    var languageCode = NormalizeLanguageCode(guide.LanguageCode);
                    var script = guide.Script.Trim();

                    updatedPoi.NarrationTranslations[languageCode] = script;
                    if (string.Equals(languageCode, "vi", StringComparison.OrdinalIgnoreCase))
                    {
                        updatedPoi.NarrationText = script;
                    }
                }

                var preferredFileGuide = poiGuides
                    .Where(item => string.Equals(item.SourceType, "file", StringComparison.OrdinalIgnoreCase) &&
                                   !string.IsNullOrWhiteSpace(item.FilePath))
                    .OrderBy(item => string.Equals(NormalizeLanguageCode(item.LanguageCode), "vi", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .FirstOrDefault();

                if (preferredFileGuide is not null)
                {
                    updatedPoi.AudioAssetPath = preferredFileGuide.FilePath.Trim();
                }

                var normalizedUpdatedPoi = Normalize(updatedPoi);
                if (!AreEquivalent(poi, normalizedUpdatedPoi))
                {
                    _pois[index] = normalizedUpdatedPoi;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                SaveUnsafe();
            }
        }
    }

    private List<PoiDto> LoadPois()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<PoiDto>>(json, _jsonOptions);
                if (items is not null)
                {
                    return items.Select(Normalize).ToList();
                }
            }
            catch
            {
                // Fall back to the seed data if the file is missing or invalid.
            }
        }

        var seeded = PoiSeedData.CreateDefaultDtos()
            .Select(dto => dto.Clone())
            .Select(Normalize)
            .ToList();

        _pois = seeded;
        SaveUnsafe();

        return seeded;
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_pois, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static PoiDto Normalize(PoiDto dto)
    {
        var normalized = dto.Clone();
        normalized.NarrationTranslations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        normalized.Code ??= string.Empty;
        normalized.Name ??= string.Empty;
        normalized.Category = string.IsNullOrWhiteSpace(normalized.Category) ? "Ẩm thực" : normalized.Category;
        normalized.ImageSource ??= string.Empty;
        normalized.Address ??= string.Empty;
        normalized.Description ??= string.Empty;
        normalized.SpecialDish ??= string.Empty;
        normalized.PriceRange ??= string.Empty;
        normalized.OpeningHours ??= string.Empty;
        normalized.FirstDishSuggestion ??= string.Empty;
        normalized.FeaturedCategories = (normalized.FeaturedCategories ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        normalized.NarrationText ??= string.Empty;
        normalized.MapLink ??= string.Empty;
        normalized.AudioAssetPath ??= string.Empty;
        normalized.CooldownMinutes = normalized.CooldownMinutes <= 0 ? 5 : normalized.CooldownMinutes;
        normalized.TriggerRadiusMeters = normalized.TriggerRadiusMeters <= 0 ? 50 : normalized.TriggerRadiusMeters;
        normalized.Priority = normalized.Priority <= 0 ? 1 : normalized.Priority;

        return normalized;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        var normalized = (languageCode ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.StartsWith("vi", StringComparison.Ordinal))
        {
            return "vi";
        }

        if (normalized.StartsWith("en", StringComparison.Ordinal))
        {
            return "en";
        }

        if (normalized.StartsWith("zh", StringComparison.Ordinal))
        {
            return "zh";
        }

        if (normalized.StartsWith("ko", StringComparison.Ordinal))
        {
            return "ko";
        }

        if (normalized.StartsWith("fr", StringComparison.Ordinal))
        {
            return "fr";
        }

        return string.IsNullOrWhiteSpace(normalized) ? "vi" : normalized;
    }

    private static bool AreEquivalent(PoiDto left, PoiDto right)
    {
        if (!string.Equals(left.NarrationText, right.NarrationText, StringComparison.Ordinal) ||
            !string.Equals(left.AudioAssetPath, right.AudioAssetPath, StringComparison.Ordinal))
        {
            return false;
        }

        if (left.NarrationTranslations.Count != right.NarrationTranslations.Count)
        {
            return false;
        }

        foreach (var entry in left.NarrationTranslations)
        {
            if (!right.NarrationTranslations.TryGetValue(entry.Key, out var otherValue) ||
                !string.Equals(entry.Value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

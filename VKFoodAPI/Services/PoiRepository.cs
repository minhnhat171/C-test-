using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;

namespace VKFoodAPI.Services;

public class PoiRepository
{
    private static readonly string[] SupportedNarrationLanguages = ["vi", "en", "zh", "ko", "fr"];

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private readonly IReadOnlyDictionary<Guid, PoiDto> _seedPois;
    private List<PoiDto> _pois;

    public PoiRepository(IHostEnvironment environment)
    {
        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "pois.json");
        _seedPois = BuildSeedPois();
        _pois = LoadPois();
    }

    public IReadOnlyList<PoiDto> GetAll()
    {
        lock (_syncRoot)
        {
            return _pois
                .Where(dto => !dto.IsDeleted)
                .Select(dto => dto.Clone())
                .ToList();
        }
    }

    public PoiDto? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            return _pois.FirstOrDefault(x => x.Id == id && !x.IsDeleted)?.Clone();
        }
    }

    public PoiDto Create(PoiDto dto)
    {
        lock (_syncRoot)
        {
            var created = Normalize(dto);
            created.Id = created.Id == Guid.Empty ? Guid.NewGuid() : created.Id;
            created.IsDeleted = false;
            created.DeletedAtUtc = null;
            created.CreatedAtUtc = DateTime.UtcNow;
            created.UpdatedAtUtc = created.CreatedAtUtc;

            ValidateForSave(created);

            _pois.Add(created);
            SaveUnsafe();

            return created.Clone();
        }
    }

    public bool Update(Guid id, PoiDto dto)
    {
        lock (_syncRoot)
        {
            var index = _pois.FindIndex(x => x.Id == id && !x.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(dto);
            updated.Id = id;
            updated.IsDeleted = false;
            updated.DeletedAtUtc = null;
            updated.CreatedAtUtc = _pois[index].CreatedAtUtc;
            updated.UpdatedAtUtc = DateTime.UtcNow;

            ValidateForSave(updated, id);

            _pois[index] = updated;
            SaveUnsafe();

            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var index = _pois.FindIndex(x => x.Id == id && !x.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var deleted = _pois[index].Clone();
            deleted.IsActive = false;
            deleted.IsDeleted = true;
            deleted.DeletedAtUtc = DateTime.UtcNow;
            deleted.UpdatedAtUtc = deleted.DeletedAtUtc.Value;

            _pois[index] = deleted;
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
                if (poi.IsDeleted)
                {
                    continue;
                }

                var updatedPoi = poi.Clone();
                updatedPoi.AudioAssetPath = string.Empty;
                updatedPoi.AudioAssetPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (!groupedGuides.TryGetValue(poi.Id, out var poiGuides) || poiGuides.Count == 0)
                {
                    var normalizedWithoutAudio = Normalize(updatedPoi);
                    if (!AreEquivalent(poi, normalizedWithoutAudio))
                    {
                        normalizedWithoutAudio.UpdatedAtUtc = DateTime.UtcNow;
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
                    var script = LegacyTextRepair.Clean(guide.Script);

                    if (!string.IsNullOrWhiteSpace(script))
                    {
                        updatedPoi.NarrationTranslations[languageCode] = script;
                        if (string.Equals(languageCode, "vi", StringComparison.OrdinalIgnoreCase))
                        {
                            updatedPoi.NarrationText = script;
                        }
                    }
                }

                var fileGuides = poiGuides
                    .Where(item => string.Equals(item.SourceType, "file", StringComparison.OrdinalIgnoreCase) &&
                                   !string.IsNullOrWhiteSpace(item.FilePath))
                    .GroupBy(item => NormalizeLanguageCode(item.LanguageCode), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group
                        .OrderByDescending(item => item.UpdatedAtUtc)
                        .First())
                    .ToList();

                foreach (var guide in fileGuides)
                {
                    var languageCode = NormalizeLanguageCode(guide.LanguageCode);
                    var filePath = RepairUrl(guide.FilePath);

                    if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(filePath))
                    {
                        updatedPoi.AudioAssetPaths[languageCode] = filePath;
                    }
                }

                var preferredFilePath = fileGuides
                    .OrderBy(item => string.Equals(NormalizeLanguageCode(item.LanguageCode), "vi", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenByDescending(item => item.UpdatedAtUtc)
                    .Select(item => RepairUrl(item.FilePath))
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

                if (!string.IsNullOrWhiteSpace(preferredFilePath))
                {
                    updatedPoi.AudioAssetPath = preferredFilePath;
                }

                var normalizedUpdatedPoi = Normalize(updatedPoi);
                if (!AreEquivalent(poi, normalizedUpdatedPoi))
                {
                    normalizedUpdatedPoi.UpdatedAtUtc = DateTime.UtcNow;
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
                    var normalizedItems = items.Select(Normalize).ToList();
                    _pois = normalizedItems;
                    SaveUnsafe();
                    return normalizedItems;
                }
            }
            catch
            {
                // Fall back to the seed data if the file is missing or invalid.
            }
        }

        var seeded = _seedPois.Values
            .Select(dto => dto.Clone())
            .OrderBy(dto => dto.Code, StringComparer.OrdinalIgnoreCase)
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

    private PoiDto Normalize(PoiDto dto)
    {
        var seedPoi = GetSeedPoi(dto.Id);
        var normalized = dto.Clone();
        normalized.NarrationTranslations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        normalized.AudioAssetPaths ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        normalized.Code = RepairText(normalized.Code, seedPoi?.Code);
        normalized.Name = RepairText(normalized.Name, seedPoi?.Name);
        normalized.Category = RepairText(normalized.Category, seedPoi?.Category, "Ẩm thực");
        normalized.ImageSource = RepairText(normalized.ImageSource, seedPoi?.ImageSource);
        normalized.Address = RepairText(normalized.Address, seedPoi?.Address);
        normalized.Description = RepairText(normalized.Description, seedPoi?.Description);
        normalized.SpecialDish = RepairText(normalized.SpecialDish, seedPoi?.SpecialDish);
        normalized.PriceRange = RepairText(normalized.PriceRange, seedPoi?.PriceRange);
        normalized.OpeningHours = RepairText(normalized.OpeningHours, seedPoi?.OpeningHours);
        normalized.FirstDishSuggestion = RepairText(normalized.FirstDishSuggestion, seedPoi?.FirstDishSuggestion);
        normalized.FeaturedCategories = (normalized.FeaturedCategories ?? seedPoi?.FeaturedCategories ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => LegacyTextRepair.Clean(item).Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalized.FeaturedCategories.Count == 0 && seedPoi is not null)
        {
            normalized.FeaturedCategories = seedPoi.FeaturedCategories
                .Select(item => item.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        normalized.MapLink = RepairUrl(normalized.MapLink, seedPoi?.MapLink);
        normalized.AudioAssetPath = RepairUrl(normalized.AudioAssetPath, seedPoi?.AudioAssetPath);
        normalized.AudioAssetPaths = NormalizeAudioAssetPaths(normalized.AudioAssetPaths, seedPoi?.AudioAssetPaths);
        normalized.OwnerUserCode = RepairText(normalized.OwnerUserCode, seedPoi?.OwnerUserCode);
        normalized.OwnerDisplayName = RepairText(normalized.OwnerDisplayName, seedPoi?.OwnerDisplayName);
        normalized.OwnerEmail = NormalizeEmail(RepairText(normalized.OwnerEmail, seedPoi?.OwnerEmail));
        normalized.CooldownMinutes = normalized.CooldownMinutes <= 0
            ? seedPoi?.CooldownMinutes > 0
                ? seedPoi.CooldownMinutes
                : 5
            : normalized.CooldownMinutes;
        normalized.TriggerRadiusMeters = normalized.TriggerRadiusMeters <= 0
            ? seedPoi?.TriggerRadiusMeters > 0
                ? seedPoi.TriggerRadiusMeters
                : 50
            : normalized.TriggerRadiusMeters;
        normalized.Priority = normalized.Priority <= 0
            ? seedPoi?.Priority > 0
                ? seedPoi.Priority
                : 1
            : normalized.Priority;
        normalized.CreatedAtUtc = NormalizeUtc(normalized.CreatedAtUtc);
        normalized.UpdatedAtUtc = NormalizeUtc(normalized.UpdatedAtUtc);
        normalized.DeletedAtUtc = NormalizeUtc(normalized.DeletedAtUtc);
        normalized.NarrationTranslations = NormalizeNarrationTranslations(normalized, seedPoi);
        normalized.NarrationText = ResolveNarrationText(normalized, seedPoi);
        if (normalized.IsDeleted && normalized.DeletedAtUtc is null)
        {
            normalized.DeletedAtUtc = DateTime.UtcNow;
        }

        return normalized;
    }

    private void ValidateForSave(PoiDto dto, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new ArgumentException("POI code is required.", nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("POI name is required.", nameof(dto));
        }

        if (dto.Latitude < -90 || dto.Latitude > 90)
        {
            throw new ArgumentException("POI latitude must be between -90 and 90.", nameof(dto));
        }

        if (dto.Longitude < -180 || dto.Longitude > 180)
        {
            throw new ArgumentException("POI longitude must be between -180 and 180.", nameof(dto));
        }

        if (_pois.Any(item =>
                !item.IsDeleted &&
                (!currentId.HasValue || item.Id != currentId.Value) &&
                string.Equals(item.Code, dto.Code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"POI code '{dto.Code}' already exists.");
        }
    }

    private PoiDto? GetSeedPoi(Guid id)
    {
        return id != Guid.Empty && _seedPois.TryGetValue(id, out var seedPoi)
            ? seedPoi
            : null;
    }

    private static IReadOnlyDictionary<Guid, PoiDto> BuildSeedPois()
    {
        return PoiSeedData.CreateDefaultDtos()
            .Select(NormalizeSeedPoi)
            .Where(dto => dto.Id != Guid.Empty)
            .GroupBy(dto => dto.Id)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static PoiDto NormalizeSeedPoi(PoiDto dto)
    {
        var normalized = dto.Clone();
        normalized.NarrationTranslations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        normalized.AudioAssetPaths ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        normalized.Code = LegacyTextRepair.Clean(normalized.Code);
        normalized.Name = LegacyTextRepair.Clean(normalized.Name);
        normalized.Category = LegacyTextRepair.Clean(normalized.Category);
        normalized.ImageSource = LegacyTextRepair.Clean(normalized.ImageSource);
        normalized.Address = LegacyTextRepair.Clean(normalized.Address);
        normalized.Description = LegacyTextRepair.Clean(normalized.Description);
        normalized.SpecialDish = LegacyTextRepair.Clean(normalized.SpecialDish);
        normalized.PriceRange = LegacyTextRepair.Clean(normalized.PriceRange);
        normalized.OpeningHours = LegacyTextRepair.Clean(normalized.OpeningHours);
        normalized.FirstDishSuggestion = LegacyTextRepair.Clean(normalized.FirstDishSuggestion);
        normalized.FeaturedCategories = (normalized.FeaturedCategories ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => LegacyTextRepair.Clean(item).Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        normalized.MapLink = LegacyTextRepair.Clean(normalized.MapLink);
        normalized.AudioAssetPath = LegacyTextRepair.Clean(normalized.AudioAssetPath);
        normalized.AudioAssetPaths = NormalizeAudioAssetPaths(normalized.AudioAssetPaths);
        normalized.OwnerUserCode = LegacyTextRepair.Clean(normalized.OwnerUserCode);
        normalized.OwnerDisplayName = LegacyTextRepair.Clean(normalized.OwnerDisplayName);
        normalized.OwnerEmail = NormalizeEmail(LegacyTextRepair.Clean(normalized.OwnerEmail));
        normalized.Category = string.IsNullOrWhiteSpace(normalized.Category) ? "Ẩm thực" : normalized.Category;
        normalized.CooldownMinutes = normalized.CooldownMinutes <= 0 ? 5 : normalized.CooldownMinutes;
        normalized.TriggerRadiusMeters = normalized.TriggerRadiusMeters <= 0 ? 50 : normalized.TriggerRadiusMeters;
        normalized.Priority = normalized.Priority <= 0 ? 1 : normalized.Priority;
        normalized.CreatedAtUtc = NormalizeUtc(normalized.CreatedAtUtc);
        normalized.UpdatedAtUtc = NormalizeUtc(normalized.UpdatedAtUtc);
        normalized.DeletedAtUtc = NormalizeUtc(normalized.DeletedAtUtc);
        normalized.NarrationTranslations = BuildLanguageLookup(normalized.NarrationTranslations);
        normalized.NarrationText = ResolveSeedNarrationText(normalized.NarrationText, normalized.NarrationTranslations);
        return normalized;
    }

    private Dictionary<string, string> NormalizeNarrationTranslations(PoiDto poi, PoiDto? seedPoi)
    {
        var currentTranslations = BuildLanguageLookup(poi.NarrationTranslations);
        var seedTranslations = seedPoi?.NarrationTranslations is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(seedPoi.NarrationTranslations, StringComparer.OrdinalIgnoreCase);
        var languages = SupportedNarrationLanguages
            .Concat(currentTranslations.Keys)
            .Concat(seedTranslations.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var languageCode in languages)
        {
            currentTranslations.TryGetValue(languageCode, out var currentValue);
            seedTranslations.TryGetValue(languageCode, out var seedValue);

            var repaired = RepairText(currentValue, seedValue);
            if (string.IsNullOrWhiteSpace(repaired))
            {
                repaired = BuildFallbackNarration(languageCode, poi, seedPoi);
            }

            if (!string.IsNullOrWhiteSpace(repaired))
            {
                translations[NormalizeLanguageCode(languageCode)] = repaired;
            }
        }

        return translations;
    }

    private string ResolveNarrationText(PoiDto poi, PoiDto? seedPoi)
    {
        var repaired = RepairText(poi.NarrationText, seedPoi?.NarrationText);
        if (!LegacyTextRepair.NeedsSeedFallback(repaired))
        {
            return repaired;
        }

        if (poi.NarrationTranslations.TryGetValue("vi", out var vietnameseNarration) &&
            !string.IsNullOrWhiteSpace(vietnameseNarration))
        {
            return vietnameseNarration;
        }

        return BuildFallbackNarration("vi", poi, seedPoi);
    }

    private static Dictionary<string, string> BuildLanguageLookup(
        IDictionary<string, string>? translations)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (translations is null)
        {
            return result;
        }

        foreach (var entry in translations)
        {
            var languageCode = NormalizeLanguageCode(entry.Key);
            var text = LegacyTextRepair.Clean(entry.Value);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(text))
            {
                result[languageCode] = text;
            }
        }

        return result;
    }

    private static Dictionary<string, string> NormalizeAudioAssetPaths(
        IDictionary<string, string>? paths,
        IDictionary<string, string>? seedPaths = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddAudioAssetPaths(result, seedPaths);
        AddAudioAssetPaths(result, paths);

        return result;
    }

    private static void AddAudioAssetPaths(
        IDictionary<string, string> target,
        IDictionary<string, string>? paths)
    {
        if (paths is null)
        {
            return;
        }

        foreach (var entry in paths)
        {
            var languageCode = NormalizeLanguageCode(entry.Key);
            var filePath = RepairUrl(entry.Value);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(filePath))
            {
                target[languageCode] = filePath;
            }
        }
    }

    private static string ResolveSeedNarrationText(
        string? narrationText,
        IReadOnlyDictionary<string, string> translations)
    {
        var repaired = LegacyTextRepair.Clean(narrationText);
        if (!string.IsNullOrWhiteSpace(repaired))
        {
            return repaired;
        }

        return translations.TryGetValue("vi", out var vietnameseNarration)
            ? vietnameseNarration
            : string.Empty;
    }

    private static string RepairText(string? value, string? seedValue = null, string fallback = "")
    {
        var repaired = LegacyTextRepair.Clean(value);
        if (LegacyTextRepair.NeedsSeedFallback(repaired) && !string.IsNullOrWhiteSpace(seedValue))
        {
            var repairedSeed = LegacyTextRepair.Clean(seedValue);
            if (!string.IsNullOrWhiteSpace(repairedSeed))
            {
                return repairedSeed;
            }
        }

        return string.IsNullOrWhiteSpace(repaired) ? fallback : repaired;
    }

    private static string RepairUrl(string? value, string? seedValue = null)
    {
        var repaired = LegacyTextRepair.Clean(value);
        if (!string.IsNullOrWhiteSpace(repaired))
        {
            return repaired;
        }

        return LegacyTextRepair.Clean(seedValue);
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value == default)
        {
            return DateTime.UtcNow;
        }

        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value.HasValue
            ? NormalizeUtc(value.Value)
            : null;
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

    private static string BuildFallbackNarration(string languageCode, PoiDto poi, PoiDto? seedPoi)
    {
        var name = string.IsNullOrWhiteSpace(poi.Name) ? seedPoi?.Name ?? "địa điểm này" : poi.Name;
        var address = string.IsNullOrWhiteSpace(poi.Address) ? seedPoi?.Address ?? string.Empty : poi.Address;
        var description = string.IsNullOrWhiteSpace(poi.Description) ? seedPoi?.Description ?? string.Empty : poi.Description;
        var specialDish = string.IsNullOrWhiteSpace(poi.SpecialDish) ? seedPoi?.SpecialDish ?? string.Empty : poi.SpecialDish;

        return NormalizeLanguageCode(languageCode) switch
        {
            "en" => JoinSentences(
                $"You are near {name}.",
                string.IsNullOrWhiteSpace(description) ? $"Address: {address}." : description,
                string.IsNullOrWhiteSpace(specialDish) ? string.Empty : $"Signature dishes: {specialDish}."),
            "zh" => JoinSentences(
                $"您现在在{name}附近。",
                string.IsNullOrWhiteSpace(description) ? $"地址：{address}。" : description,
                string.IsNullOrWhiteSpace(specialDish) ? string.Empty : $"推荐菜品：{specialDish}。"),
            "ko" => JoinSentences(
                $"지금 {name} 근처에 있습니다.",
                string.IsNullOrWhiteSpace(description) ? $"주소는 {address}입니다." : description,
                string.IsNullOrWhiteSpace(specialDish) ? string.Empty : $"추천 메뉴는 {specialDish}입니다."),
            "fr" => JoinSentences(
                $"Vous êtes près de {name}.",
                string.IsNullOrWhiteSpace(description) ? $"Adresse : {address}." : description,
                string.IsNullOrWhiteSpace(specialDish) ? string.Empty : $"Plats recommandés : {specialDish}."),
            _ => JoinSentences(
                $"Bạn đang ở gần {name}.",
                description,
                string.IsNullOrWhiteSpace(specialDish) ? string.Empty : $"Món nên thử: {specialDish}.")
        };
    }

    private static string JoinSentences(params string[] segments)
    {
        return string.Join(
            " ",
            segments
                .Select(EnsureSentence)
                .Where(segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static string EnsureSentence(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var lastCharacter = trimmed[^1];
        return lastCharacter is '.' or '!' or '?'
            ? trimmed
            : $"{trimmed}.";
    }

    private static bool AreEquivalent(PoiDto left, PoiDto right)
    {
        if (!string.Equals(left.NarrationText, right.NarrationText, StringComparison.Ordinal) ||
            !string.Equals(left.AudioAssetPath, right.AudioAssetPath, StringComparison.Ordinal))
        {
            return false;
        }

        if (!AreDictionariesEquivalent(left.AudioAssetPaths, right.AudioAssetPaths))
        {
            return false;
        }

        return AreDictionariesEquivalent(left.NarrationTranslations, right.NarrationTranslations);
    }

    private static bool AreDictionariesEquivalent(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {
        left ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        right ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var entry in left)
        {
            if (!right.TryGetValue(entry.Key, out var otherValue) ||
                !string.Equals(entry.Value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}

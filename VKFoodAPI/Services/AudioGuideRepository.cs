using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;
using VKFoodAPI.Models;

namespace VKFoodAPI.Services;

public class AudioGuideRepository
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly PoiRepository _poiRepository;
    private readonly string _dataFilePath;
    private List<AudioGuideRecord> _audioGuides;

    public AudioGuideRepository(IHostEnvironment environment, PoiRepository poiRepository)
    {
        _poiRepository = poiRepository;

        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "audio-guides.json");
        _audioGuides = LoadAudioGuides();
        SyncPoisUnsafe();
    }

    public IReadOnlyList<AudioGuideDto> GetAll()
    {
        lock (_syncRoot)
        {
            return _audioGuides
                .Where(item => !item.IsDeleted)
                .Select(item => HydratePoiMetadata(item))
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.PoiName)
                .ThenBy(item => item.LanguageCode)
                .Select(item => item.ToDto())
                .ToList();
        }
    }

    public AudioGuideDto? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            var item = _audioGuides.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            return item is null ? null : HydratePoiMetadata(item).ToDto();
        }
    }

    public bool PoiExists(Guid poiId)
    {
        return _poiRepository.GetById(poiId) is not null;
    }

    public AudioGuideDto Create(AudioGuideDto dto)
    {
        lock (_syncRoot)
        {
            var created = Normalize(dto);
            created.Id = created.Id == Guid.Empty ? Guid.NewGuid() : created.Id;
            created.IsDeleted = false;
            created.DeletedAtUtc = null;
            created.CreatedAtUtc = DateTime.UtcNow;
            created.UpdatedAtUtc = DateTime.UtcNow;

            ValidateForSave(created);

            _audioGuides.Add(created);
            SaveUnsafe();
            SyncPoisUnsafe();

            return HydratePoiMetadata(created).ToDto();
        }
    }

    public bool Update(Guid id, AudioGuideDto dto)
    {
        lock (_syncRoot)
        {
            var index = _audioGuides.FindIndex(item => item.Id == id && !item.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(dto);
            updated.Id = id;
            updated.IsDeleted = false;
            updated.DeletedAtUtc = null;
            updated.CreatedAtUtc = _audioGuides[index].CreatedAtUtc;
            updated.UpdatedAtUtc = DateTime.UtcNow;

            ValidateForSave(updated, id);

            _audioGuides[index] = updated;
            SaveUnsafe();
            SyncPoisUnsafe();

            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var index = _audioGuides.FindIndex(item => item.Id == id && !item.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var deleted = _audioGuides[index].Clone();
            deleted.IsPublished = false;
            deleted.IsDeleted = true;
            deleted.DeletedAtUtc = DateTime.UtcNow;
            deleted.UpdatedAtUtc = deleted.DeletedAtUtc.Value;

            _audioGuides[index] = deleted;
            SaveUnsafe();
            SyncPoisUnsafe();
            return true;
        }
    }

    private List<AudioGuideRecord> LoadAudioGuides()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<AudioGuideRecord>>(json, _jsonOptions);
                if (items is not null)
                {
                    var normalizedItems = items
                        .Select(Normalize)
                        .ToList();
                    _audioGuides = normalizedItems;
                    SaveUnsafe();
                    return normalizedItems;
                }
            }
            catch
            {
                // Fall back to seed data when the file is missing or invalid.
            }
        }

        var seeded = AudioGuideSeedData.CreateDefaultDtos()
            .Select(AudioGuideRecord.FromDto)
            .Select(Normalize)
            .ToList();

        _audioGuides = seeded;
        SaveUnsafe();
        return seeded;
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_audioGuides, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private void SyncPoisUnsafe()
    {
        _poiRepository.ApplyPublishedAudioGuides(_audioGuides
            .Where(item => !item.IsDeleted)
            .Select(item => item.ToDto()));
    }

    private AudioGuideRecord HydratePoiMetadata(AudioGuideRecord record)
    {
        var hydrated = record.Clone();
        var poi = _poiRepository.GetById(hydrated.PoiId);

        if (poi is not null)
        {
            hydrated.PoiCode = poi.Code;
            hydrated.PoiName = poi.Name;
        }
        else
        {
            hydrated.PoiCode = LegacyTextRepair.Clean(hydrated.PoiCode);
            hydrated.PoiName = LegacyTextRepair.Clean(hydrated.PoiName);
        }

        return hydrated;
    }

    private AudioGuideRecord Normalize(AudioGuideDto dto)
    {
        return Normalize(AudioGuideRecord.FromDto(dto));
    }

    private AudioGuideRecord Normalize(AudioGuideRecord record)
    {
        var normalized = record.Clone();
        var poi = _poiRepository.GetById(normalized.PoiId);

        normalized.PoiId = poi?.Id ?? normalized.PoiId;
        normalized.PoiCode = poi?.Code ?? LegacyTextRepair.Clean(normalized.PoiCode);
        normalized.PoiName = poi?.Name ?? LegacyTextRepair.Clean(normalized.PoiName);
        normalized.LanguageCode = NormalizeLanguageCode(normalized.LanguageCode);
        normalized.VoiceType = NormalizeVoiceType(normalized.VoiceType);
        normalized.SourceType = NormalizeSourceType(normalized.SourceType);
        normalized.Script = RepairScript(normalized.Script, poi, normalized.LanguageCode);
        normalized.FilePath = NormalizeFilePath(LegacyTextRepair.Clean(normalized.FilePath));
        normalized.EstimatedSeconds = normalized.EstimatedSeconds <= 0
            ? EstimateSeconds(normalized.Script)
            : normalized.EstimatedSeconds;
        normalized.CreatedAtUtc = NormalizeUtc(normalized.CreatedAtUtc);
        normalized.UpdatedAtUtc = normalized.UpdatedAtUtc == default
            ? DateTime.UtcNow
            : normalized.UpdatedAtUtc.ToUniversalTime();
        normalized.DeletedAtUtc = NormalizeUtc(normalized.DeletedAtUtc);
        if (normalized.IsDeleted && normalized.DeletedAtUtc is null)
        {
            normalized.DeletedAtUtc = DateTime.UtcNow;
        }

        if (string.Equals(normalized.SourceType, "tts", StringComparison.OrdinalIgnoreCase))
        {
            normalized.FilePath = string.Empty;
            if (string.IsNullOrWhiteSpace(normalized.Script))
            {
                normalized.Script = ResolvePoiNarration(poi, normalized.LanguageCode);
                normalized.EstimatedSeconds = EstimateSeconds(normalized.Script);
            }
        }

        return normalized;
    }

    private void ValidateForSave(AudioGuideRecord record, Guid? currentId = null)
    {
        if (record.PoiId == Guid.Empty || _poiRepository.GetById(record.PoiId) is null)
        {
            throw new ArgumentException("Audio guide must reference an active POI.", nameof(record));
        }

        if (string.Equals(record.SourceType, "file", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(record.FilePath))
            {
                throw new ArgumentException("FilePath is required when SourceType is file.", nameof(record));
            }
        }
        else if (string.IsNullOrWhiteSpace(record.Script))
        {
            throw new ArgumentException("Script is required when SourceType is tts.", nameof(record));
        }

        if (_audioGuides.Any(item =>
                !item.IsDeleted &&
                (!currentId.HasValue || item.Id != currentId.Value) &&
                item.PoiId == record.PoiId &&
                string.Equals(item.LanguageCode, record.LanguageCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.SourceType, record.SourceType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Audio guide for POI '{record.PoiCode}' language '{record.LanguageCode}' source '{record.SourceType}' already exists.");
        }
    }

    private static string RepairScript(string? script, PoiDto? poi, string languageCode)
    {
        var repaired = LegacyTextRepair.Clean(script);
        if (!LegacyTextRepair.NeedsSeedFallback(repaired))
        {
            return repaired;
        }

        return ResolvePoiNarration(poi, languageCode);
    }

    private static string ResolvePoiNarration(PoiDto? poi, string languageCode)
    {
        if (poi is null)
        {
            return string.Empty;
        }

        if (poi.NarrationTranslations.TryGetValue(languageCode, out var localizedNarration) &&
            !string.IsNullOrWhiteSpace(localizedNarration))
        {
            return localizedNarration;
        }

        if (poi.NarrationTranslations.TryGetValue("vi", out var vietnameseNarration) &&
            !string.IsNullOrWhiteSpace(vietnameseNarration))
        {
            return vietnameseNarration;
        }

        return poi.NarrationText;
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

    private static string NormalizeVoiceType(string? voiceType)
    {
        var normalized = (voiceType ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "male" => "male",
            "neutral" => "neutral",
            _ => "female"
        };
    }

    private static string NormalizeSourceType(string? sourceType)
    {
        return string.Equals(sourceType?.Trim(), "file", StringComparison.OrdinalIgnoreCase)
            ? "file"
            : "tts";
    }

    private static string NormalizeFilePath(string? filePath)
    {
        return (filePath ?? string.Empty)
            .Trim()
            .TrimStart('\\')
            .Replace('\\', '/');
    }

    private static int EstimateSeconds(string? script)
    {
        var wordCount = (script ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return wordCount == 0 ? 30 : Math.Max(15, (int)Math.Ceiling(wordCount / 2.5));
    }
}

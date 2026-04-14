using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;

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
    private List<AudioGuideDto> _audioGuides;

    public AudioGuideRepository(IHostEnvironment environment, PoiRepository poiRepository)
    {
        _poiRepository = poiRepository;

        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
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
                .Select(item => HydratePoiMetadata(item))
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.PoiName)
                .ThenBy(item => item.LanguageCode)
                .Select(item => item.Clone())
                .ToList();
        }
    }

    public AudioGuideDto? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            var item = _audioGuides.FirstOrDefault(x => x.Id == id);
            return item is null ? null : HydratePoiMetadata(item).Clone();
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
            created.UpdatedAtUtc = DateTime.UtcNow;

            _audioGuides.Add(created);
            SaveUnsafe();
            SyncPoisUnsafe();

            return HydratePoiMetadata(created).Clone();
        }
    }

    public bool Update(Guid id, AudioGuideDto dto)
    {
        lock (_syncRoot)
        {
            var index = _audioGuides.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(dto);
            updated.Id = id;
            updated.UpdatedAtUtc = DateTime.UtcNow;

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
            var removed = _audioGuides.RemoveAll(item => item.Id == id);
            if (removed == 0)
            {
                return false;
            }

            SaveUnsafe();
            SyncPoisUnsafe();
            return true;
        }
    }

    private List<AudioGuideDto> LoadAudioGuides()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<AudioGuideDto>>(json, _jsonOptions);
                if (items is not null)
                {
                    return items
                        .Select(Normalize)
                        .ToList();
                }
            }
            catch
            {
                // Fall back to seed data when the file is missing or invalid.
            }
        }

        var seeded = AudioGuideSeedData.CreateDefaultDtos()
            .Select(item => item.Clone())
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
        _poiRepository.ApplyPublishedAudioGuides(_audioGuides);
    }

    private AudioGuideDto HydratePoiMetadata(AudioGuideDto dto)
    {
        var hydrated = dto.Clone();
        var poi = _poiRepository.GetById(hydrated.PoiId);

        if (poi is not null)
        {
            hydrated.PoiCode = poi.Code;
            hydrated.PoiName = poi.Name;
        }

        return hydrated;
    }

    private AudioGuideDto Normalize(AudioGuideDto dto)
    {
        var normalized = dto.Clone();
        var poi = _poiRepository.GetById(normalized.PoiId);

        normalized.PoiId = poi?.Id ?? normalized.PoiId;
        normalized.PoiCode = poi?.Code ?? string.Empty;
        normalized.PoiName = poi?.Name ?? string.Empty;
        normalized.LanguageCode = NormalizeLanguageCode(normalized.LanguageCode);
        normalized.VoiceType = NormalizeVoiceType(normalized.VoiceType);
        normalized.SourceType = NormalizeSourceType(normalized.SourceType);
        normalized.Script = normalized.Script?.Trim() ?? string.Empty;
        normalized.FilePath = NormalizeFilePath(normalized.FilePath);
        normalized.EstimatedSeconds = normalized.EstimatedSeconds <= 0
            ? EstimateSeconds(normalized.Script)
            : normalized.EstimatedSeconds;
        normalized.UpdatedAtUtc = normalized.UpdatedAtUtc == default
            ? DateTime.UtcNow
            : normalized.UpdatedAtUtc.ToUniversalTime();

        if (string.Equals(normalized.SourceType, "tts", StringComparison.OrdinalIgnoreCase))
        {
            normalized.FilePath = string.Empty;
        }

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

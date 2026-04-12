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
        normalized.NarrationText ??= string.Empty;
        normalized.MapLink ??= string.Empty;
        normalized.AudioAssetPath ??= string.Empty;
        normalized.CooldownMinutes = normalized.CooldownMinutes <= 0 ? 5 : normalized.CooldownMinutes;
        normalized.TriggerRadiusMeters = normalized.TriggerRadiusMeters <= 0 ? 50 : normalized.TriggerRadiusMeters;
        normalized.Priority = normalized.Priority <= 0 ? 1 : normalized.Priority;

        return normalized;
    }
}

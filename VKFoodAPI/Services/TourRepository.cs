using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public class TourRepository
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly PoiRepository _poiRepository;
    private readonly string _dataFilePath;
    private List<TourDto> _tours = [];

    public TourRepository(IHostEnvironment environment, PoiRepository poiRepository)
    {
        _poiRepository = poiRepository;

        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "tours.json");
        _tours = LoadTours();
        SyncPoisUnsafe();
    }

    public IReadOnlyList<TourDto> GetAll()
    {
        lock (_syncRoot)
        {
            SyncPoisUnsafe();

            return _tours
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.Name)
                .Select(item => item.Clone())
                .ToList();
        }
    }

    public TourDto? GetById(int id)
    {
        lock (_syncRoot)
        {
            SyncPoisUnsafe();
            return _tours.FirstOrDefault(item => item.Id == id)?.Clone();
        }
    }

    public TourDto Create(TourDto dto)
    {
        lock (_syncRoot)
        {
            SyncPoisUnsafe();

            var created = Normalize(dto);
            created.Id = created.Id > 0 ? created.Id : GetNextIdUnsafe();
            created.UpdatedAtUtc = DateTime.UtcNow;

            _tours.Add(created);
            SaveUnsafe();

            return created.Clone();
        }
    }

    public bool Update(int id, TourDto dto)
    {
        lock (_syncRoot)
        {
            SyncPoisUnsafe();

            var index = _tours.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(dto);
            updated.Id = id;
            updated.UpdatedAtUtc = DateTime.UtcNow;

            _tours[index] = updated;
            SaveUnsafe();

            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_syncRoot)
        {
            var removed = _tours.RemoveAll(item => item.Id == id);
            if (removed == 0)
            {
                return false;
            }

            SaveUnsafe();
            return true;
        }
    }

    private List<TourDto> LoadTours()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<TourDto>>(json, _jsonOptions);
                if (items is not null)
                {
                    return items
                        .Select(Normalize)
                        .ToList();
                }
            }
            catch
            {
                // Fall back to an empty list when the file is missing or invalid.
            }
        }

        _tours = [];
        SaveUnsafe();
        return [];
    }

    private void SyncPoisUnsafe()
    {
        var hasChanges = false;

        for (var index = 0; index < _tours.Count; index++)
        {
            var normalized = Normalize(_tours[index]);

            if (!AreEquivalent(_tours[index], normalized))
            {
                _tours[index] = normalized;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            SaveUnsafe();
        }
    }

    private int GetNextIdUnsafe()
    {
        return _tours.Count == 0
            ? 1
            : _tours.Max(item => item.Id) + 1;
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_tours, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private TourDto Normalize(TourDto dto)
    {
        var normalized = dto.Clone();

        normalized.Code = string.IsNullOrWhiteSpace(normalized.Code)
            ? $"VK-TOUR-{(normalized.Id > 0 ? normalized.Id : GetNextIdUnsafe()):D2}"
            : normalized.Code.Trim();
        normalized.Name = normalized.Name?.Trim() ?? string.Empty;
        normalized.Description = normalized.Description?.Trim() ?? string.Empty;
        normalized.EstimatedMinutes = normalized.EstimatedMinutes <= 0 ? 45 : normalized.EstimatedMinutes;
        normalized.UpdatedAtUtc = normalized.UpdatedAtUtc == default
            ? DateTime.UtcNow
            : normalized.UpdatedAtUtc.ToUniversalTime();
        normalized.PoiIds = (normalized.PoiIds ?? [])
            .Where(poiId => poiId != Guid.Empty && _poiRepository.GetById(poiId) is not null)
            .Distinct()
            .ToList();

        return normalized;
    }

    private static bool AreEquivalent(TourDto left, TourDto right)
    {
        if (!string.Equals(left.Code, right.Code, StringComparison.Ordinal) ||
            !string.Equals(left.Name, right.Name, StringComparison.Ordinal) ||
            !string.Equals(left.Description, right.Description, StringComparison.Ordinal) ||
            left.EstimatedMinutes != right.EstimatedMinutes ||
            left.IsActive != right.IsActive ||
            left.IsQrEnabled != right.IsQrEnabled ||
            left.PoiIds.Count != right.PoiIds.Count)
        {
            return false;
        }

        for (var index = 0; index < left.PoiIds.Count; index++)
        {
            if (left.PoiIds[index] != right.PoiIds[index])
            {
                return false;
            }
        }

        return true;
    }
}

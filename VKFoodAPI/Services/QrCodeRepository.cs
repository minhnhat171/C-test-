using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public sealed class QrCodeRepository
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly PoiRepository _poiRepository;
    private readonly TourRepository _tourRepository;
    private readonly string _dataFilePath;
    private List<QrCodeItemDto> _items;

    public QrCodeRepository(
        IHostEnvironment environment,
        PoiRepository poiRepository,
        TourRepository tourRepository)
    {
        _poiRepository = poiRepository;
        _tourRepository = tourRepository;

        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "qr-codes.json");
        _items = LoadItems();
    }

    public IReadOnlyList<QrCodeItemDto> GetAll()
    {
        lock (_syncRoot)
        {
            return _items
                .Where(item => !item.IsDeleted)
                .OrderBy(item => item.TargetType)
                .ThenBy(item => item.Code)
                .Select(item => item.Clone())
                .ToList();
        }
    }

    public QrCodeItemDto? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            return _items.FirstOrDefault(item => item.Id == id && !item.IsDeleted)?.Clone();
        }
    }

    public QrCodeItemDto? GetActiveByCode(string? code)
    {
        var normalizedCode = NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        lock (_syncRoot)
        {
            return _items.FirstOrDefault(item =>
                    !item.IsDeleted &&
                    item.IsActive &&
                    string.Equals(item.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
                ?.Clone();
        }
    }

    public QrCodeItemDto Create(QrCodeItemSaveRequest request)
    {
        lock (_syncRoot)
        {
            var created = Normalize(new QrCodeItemDto
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });

            ValidateForSave(created);

            _items.Add(created);
            SaveUnsafe();

            return created.Clone();
        }
    }

    public bool Update(Guid id, QrCodeItemSaveRequest request)
    {
        lock (_syncRoot)
        {
            var index = _items.FindIndex(item => item.Id == id && !item.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var updated = Normalize(new QrCodeItemDto
            {
                Id = id,
                Code = request.Code,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedAtUtc = _items[index].CreatedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });

            ValidateForSave(updated, id);

            _items[index] = updated;
            SaveUnsafe();

            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var index = _items.FindIndex(item => item.Id == id && !item.IsDeleted);
            if (index < 0)
            {
                return false;
            }

            var deleted = _items[index].Clone();
            deleted.IsActive = false;
            deleted.IsDeleted = true;
            deleted.DeletedAtUtc = DateTimeOffset.UtcNow;
            deleted.UpdatedAtUtc = deleted.DeletedAtUtc.Value;
            _items[index] = deleted;

            SaveUnsafe();
            return true;
        }
    }

    private List<QrCodeItemDto> LoadItems()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<QrCodeItemDto>>(json, _jsonOptions);
                if (items is not null)
                {
                    var normalizedItems = items
                        .Select(Normalize)
                        .Where(item => !string.IsNullOrWhiteSpace(item.Code))
                        .ToList();
                    _items = normalizedItems;
                    SaveUnsafe();
                    return normalizedItems;
                }
            }
            catch
            {
                // Fall back to generated defaults when the file is missing or invalid.
            }
        }

        var seeded = CreateDefaultItems();
        _items = seeded;
        SaveUnsafe();
        return seeded;
    }

    private List<QrCodeItemDto> CreateDefaultItems()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var items = new List<QrCodeItemDto>();

        items.AddRange(_poiRepository.GetAll()
            .Where(poi => poi.IsActive)
            .Select(poi => Normalize(new QrCodeItemDto
            {
                Id = Guid.NewGuid(),
                Code = string.IsNullOrWhiteSpace(poi.Code)
                    ? $"QR-POI-{poi.Id.ToString("N")[..8].ToUpperInvariant()}"
                    : poi.Code,
                TargetType = QrTargetKinds.Poi,
                TargetId = poi.Id.ToString(),
                DisplayName = poi.Name,
                Description = poi.Address,
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            })));

        items.AddRange(_tourRepository.GetAll()
            .Where(tour => tour.IsActive && tour.IsQrEnabled)
            .Select(tour => Normalize(new QrCodeItemDto
            {
                Id = Guid.NewGuid(),
                Code = string.IsNullOrWhiteSpace(tour.Code)
                    ? $"QR-TOUR-{tour.Id.ToString("D3", CultureInfo.InvariantCulture)}"
                    : tour.Code,
                TargetType = QrTargetKinds.Tour,
                TargetId = tour.Id.ToString(CultureInfo.InvariantCulture),
                DisplayName = tour.Name,
                Description = tour.Description,
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            })));

        return items
            .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private QrCodeItemDto Normalize(QrCodeItemDto item)
    {
        var normalized = item.Clone();
        normalized.Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id;
        normalized.Code = NormalizeCode(normalized.Code);
        normalized.TargetType = QrTargetKinds.Normalize(normalized.TargetType);
        normalized.TargetId = normalized.TargetId?.Trim() ?? string.Empty;
        normalized.DisplayName = RepairText(normalized.DisplayName);
        normalized.Description = RepairText(normalized.Description);
        normalized.CreatedAtUtc = NormalizeUtc(normalized.CreatedAtUtc);
        normalized.UpdatedAtUtc = NormalizeUtc(normalized.UpdatedAtUtc);
        normalized.DeletedAtUtc = normalized.DeletedAtUtc.HasValue
            ? NormalizeUtc(normalized.DeletedAtUtc.Value)
            : null;

        if (string.IsNullOrWhiteSpace(normalized.DisplayName))
        {
            normalized.DisplayName = ResolveTargetDisplayName(normalized.TargetType, normalized.TargetId);
        }

        if (string.IsNullOrWhiteSpace(normalized.Description) ||
            LegacyTextRepair.NeedsSeedFallback(normalized.Description))
        {
            normalized.Description = ResolveTargetDescription(normalized.TargetType, normalized.TargetId);
        }

        return normalized;
    }

    private void ValidateForSave(QrCodeItemDto item, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            throw new ArgumentException("QR code is required.", nameof(item));
        }

        if (string.IsNullOrWhiteSpace(item.TargetId))
        {
            throw new ArgumentException("QR target id is required.", nameof(item));
        }

        if (!TargetExists(item.TargetType, item.TargetId))
        {
            throw new ArgumentException("QR target does not exist or is inactive.", nameof(item));
        }

        var duplicate = _items.Any(existing =>
            !existing.IsDeleted &&
            (!currentId.HasValue || existing.Id != currentId.Value) &&
            string.Equals(existing.Code, item.Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException($"QR code '{item.Code}' already exists.");
        }
    }

    private bool TargetExists(string targetType, string targetId)
    {
        if (string.Equals(targetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId) &&
                   _tourRepository.GetById(tourId) is { IsActive: true, IsQrEnabled: true };
        }

        return Guid.TryParse(targetId, out var poiId) &&
               _poiRepository.GetById(poiId) is { IsActive: true };
    }

    private string ResolveTargetDisplayName(string targetType, string targetId)
    {
        if (string.Equals(targetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
        {
            return _tourRepository.GetById(tourId)?.Name ?? string.Empty;
        }

        return Guid.TryParse(targetId, out var poiId)
            ? _poiRepository.GetById(poiId)?.Name ?? string.Empty
            : string.Empty;
    }

    private string ResolveTargetDescription(string targetType, string targetId)
    {
        if (string.Equals(targetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
        {
            return RepairText(_tourRepository.GetById(tourId)?.Description);
        }

        return Guid.TryParse(targetId, out var poiId)
            ? RepairText(_poiRepository.GetById(poiId)?.Address)
            : string.Empty;
    }

    private static DateTimeOffset NormalizeUtc(DateTimeOffset value)
    {
        return value == default
            ? DateTimeOffset.UtcNow
            : value.ToUniversalTime();
    }

    private static string NormalizeCode(string? code)
    {
        return code?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private static string RepairText(string? value)
    {
        var repaired = LegacyTextRepair.Clean(value);
        return LegacyTextRepair.NeedsSeedFallback(repaired)
            ? string.Empty
            : repaired;
    }
}

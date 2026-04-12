using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public class ListeningHistoryRepository
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private List<ListeningHistoryEntryDto> _items;

    public ListeningHistoryRepository(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "listening-history.json");
        _items = LoadItems();
    }

    public IReadOnlyList<ListeningHistoryEntryDto> GetListeningHistory(string? period = null, string? sortBy = null)
    {
        lock (_syncRoot)
        {
            var query = ApplyPeriod(_items.Select(item => item.Clone()), period);
            return ApplySorting(query, sortBy).ToList();
        }
    }

    public IReadOnlyList<PoiListeningCountDto> CountListeningByPoi(string? period = null)
    {
        lock (_syncRoot)
        {
            return ApplyPeriod(_items, period)
                .GroupBy(item => new { item.PoiId, item.PoiCode, item.PoiName })
                .Select(group => new PoiListeningCountDto
                {
                    PoiId = group.Key.PoiId,
                    PoiCode = group.Key.PoiCode,
                    PoiName = group.Key.PoiName,
                    ListenCount = group.Count(),
                    CompletedCount = group.Count(item => item.Completed),
                    TotalListenSeconds = group.Sum(item => item.ListenSeconds),
                    LastStartedAtUtc = group.Max(item => item.StartedAtUtc)
                })
                .OrderByDescending(item => item.ListenCount)
                .ThenByDescending(item => item.LastStartedAtUtc)
                .ThenBy(item => item.PoiName)
                .ToList();
        }
    }

    public ListeningHistoryEntryDto Create(ListeningHistoryCreateRequest request)
    {
        lock (_syncRoot)
        {
            var created = NormalizeCreateRequest(request);
            _items.Add(created);
            SaveUnsafe();
            return created.Clone();
        }
    }

    public bool Update(Guid id, ListeningHistoryUpdateRequest request)
    {
        lock (_syncRoot)
        {
            var existing = _items.FirstOrDefault(item => item.Id == id);
            if (existing is null)
            {
                return false;
            }

            existing.ListenSeconds = Math.Max(0, request.ListenSeconds);
            existing.Completed = request.Completed;
            existing.CompletedAtUtc = request.CompletedAtUtc ?? DateTimeOffset.UtcNow;
            existing.ErrorMessage = request.ErrorMessage?.Trim() ?? string.Empty;

            SaveUnsafe();
            return true;
        }
    }

    private List<ListeningHistoryEntryDto> LoadItems()
    {
        if (!File.Exists(_dataFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var items = JsonSerializer.Deserialize<List<ListeningHistoryEntryDto>>(json, _jsonOptions);
            return items?.Select(NormalizeStoredEntry).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static IEnumerable<ListeningHistoryEntryDto> ApplyPeriod(
        IEnumerable<ListeningHistoryEntryDto> items,
        string? period)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var nowUtc = DateTimeOffset.UtcNow;

        return normalizedPeriod switch
        {
            "day" => items.Where(item => item.StartedAtUtc >= nowUtc.AddDays(-1)),
            "week" => items.Where(item => item.StartedAtUtc >= nowUtc.AddDays(-7)),
            "month" => items.Where(item => item.StartedAtUtc >= nowUtc.AddDays(-30)),
            _ => items
        };
    }

    private static IEnumerable<ListeningHistoryEntryDto> ApplySorting(
        IEnumerable<ListeningHistoryEntryDto> items,
        string? sortBy)
    {
        return NormalizeSortBy(sortBy) switch
        {
            "time_asc" => items.OrderBy(item => item.StartedAtUtc).ThenBy(item => item.PoiName),
            _ => items.OrderByDescending(item => item.StartedAtUtc).ThenBy(item => item.PoiName)
        };
    }

    private static ListeningHistoryEntryDto NormalizeCreateRequest(ListeningHistoryCreateRequest request)
    {
        return new ListeningHistoryEntryDto
        {
            Id = Guid.NewGuid(),
            PoiId = request.PoiId,
            PoiCode = request.PoiCode?.Trim() ?? string.Empty,
            PoiName = request.PoiName?.Trim() ?? string.Empty,
            UserCode = request.UserCode?.Trim() ?? "guest",
            UserDisplayName = request.UserDisplayName?.Trim() ?? "Khach",
            UserEmail = request.UserEmail?.Trim() ?? string.Empty,
            TriggerType = string.IsNullOrWhiteSpace(request.TriggerType) ? "APP" : request.TriggerType.Trim(),
            Language = string.IsNullOrWhiteSpace(request.Language) ? "vi" : request.Language.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "app" : request.Source.Trim(),
            DevicePlatform = request.DevicePlatform?.Trim() ?? string.Empty,
            AutoTriggered = request.AutoTriggered,
            StartedAtUtc = request.StartedAtUtc == default ? DateTimeOffset.UtcNow : request.StartedAtUtc,
            CompletedAtUtc = null,
            ListenSeconds = 0,
            Completed = false,
            ErrorMessage = string.Empty
        };
    }

    private static ListeningHistoryEntryDto NormalizeStoredEntry(ListeningHistoryEntryDto item)
    {
        var normalized = item.Clone();
        normalized.Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id;
        normalized.PoiCode ??= string.Empty;
        normalized.PoiName ??= string.Empty;
        normalized.UserCode = string.IsNullOrWhiteSpace(normalized.UserCode) ? "guest" : normalized.UserCode;
        normalized.UserDisplayName = string.IsNullOrWhiteSpace(normalized.UserDisplayName) ? "Khach" : normalized.UserDisplayName;
        normalized.UserEmail ??= string.Empty;
        normalized.TriggerType = string.IsNullOrWhiteSpace(normalized.TriggerType) ? "APP" : normalized.TriggerType;
        normalized.Language = string.IsNullOrWhiteSpace(normalized.Language) ? "vi" : normalized.Language;
        normalized.Source = string.IsNullOrWhiteSpace(normalized.Source) ? "app" : normalized.Source;
        normalized.DevicePlatform ??= string.Empty;
        normalized.ErrorMessage ??= string.Empty;
        normalized.ListenSeconds = Math.Max(0, normalized.ListenSeconds);
        normalized.StartedAtUtc = normalized.StartedAtUtc == default ? DateTimeOffset.UtcNow : normalized.StartedAtUtc;
        return normalized;
    }

    private static string NormalizePeriod(string? period)
    {
        return period?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            _ => "all"
        };
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "time_asc" => "time_asc",
            "oldest" => "time_asc",
            _ => "time_desc"
        };
    }
}

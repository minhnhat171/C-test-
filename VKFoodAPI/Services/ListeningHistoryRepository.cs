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
    private readonly PoiRepository _poiRepository;
    private List<ListeningHistoryEntryDto> _items;

    public ListeningHistoryRepository(IHostEnvironment environment, PoiRepository poiRepository)
    {
        _poiRepository = poiRepository;

        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "listening-history.json");
        _items = LoadItems();
    }

    public IReadOnlyList<ListeningHistoryEntryDto> GetListeningHistory(
        string? period = null,
        string? sortBy = null,
        string? userCode = null,
        string? userEmail = null,
        int? limit = null)
    {
        lock (_syncRoot)
        {
            var query = ApplyUserScope(_items.Select(item => item.Clone()), userCode, userEmail);
            query = ApplyPeriod(query, period);
            query = ApplySorting(query, sortBy);

            if (limit.HasValue && limit.Value > 0)
            {
                query = query.Take(limit.Value);
            }

            return query.ToList();
        }
    }

    public IReadOnlyList<PoiListeningCountDto> CountListeningByPoi(
        string? period = null,
        string? userCode = null,
        string? userEmail = null)
    {
        lock (_syncRoot)
        {
            var scopedItems = ApplyUserScope(_items, userCode, userEmail);

            return ApplyPeriod(scopedItems, period)
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
            var created = NormalizeCreateRequest(request, ResolvePoi(request.PoiId, request.PoiCode, request.PoiName));
            created.ReceivedAtUtc = DateTimeOffset.UtcNow;
            _items.Add(created);
            RecalculateQueuePositionsUnsafe(created.PoiId);
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
            existing.CompletedAtUtc = request.CompletedAtUtc.HasValue
                ? request.CompletedAtUtc.Value.ToUniversalTime()
                : DateTimeOffset.UtcNow;
            existing.ErrorMessage = RepairText(request.ErrorMessage);

            SaveUnsafe();
            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_syncRoot)
        {
            var affectedPoiIds = _items
                .Where(item => item.Id == id && item.PoiId != Guid.Empty)
                .Select(item => item.PoiId)
                .Distinct()
                .ToList();

            var removed = _items.RemoveAll(item => item.Id == id);
            if (removed == 0)
            {
                return false;
            }

            foreach (var poiId in affectedPoiIds)
            {
                RecalculateQueuePositionsUnsafe(poiId);
            }

            SaveUnsafe();
            return true;
        }
    }

    public int DeleteForUserScope(string? userCode, string? userEmail)
    {
        lock (_syncRoot)
        {
            if (string.IsNullOrWhiteSpace(NormalizeLookupValue(userCode)) &&
                string.IsNullOrWhiteSpace(NormalizeLookupValue(userEmail)))
            {
                return 0;
            }

            var idsToRemove = ApplyUserScope(_items, userCode, userEmail)
                .Select(item => item.Id)
                .ToHashSet();

            if (idsToRemove.Count == 0)
            {
                return 0;
            }

            var removed = _items.RemoveAll(item => idsToRemove.Contains(item.Id));
            RecalculateQueuePositions(_items);
            SaveUnsafe();
            return removed;
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
            var normalizedItems = (items ?? [])
                .Select(item => NormalizeStoredEntry(item, ResolvePoi(item.PoiId, item.PoiCode, item.PoiName)))
                .ToList();
            RecalculateQueuePositions(normalizedItems);
            _items = normalizedItems;
            SaveUnsafe();
            return normalizedItems;
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

    private static IEnumerable<ListeningHistoryEntryDto> ApplyUserScope(
        IEnumerable<ListeningHistoryEntryDto> items,
        string? userCode,
        string? userEmail)
    {
        var normalizedUserCode = NormalizeLookupValue(userCode);
        var normalizedUserEmail = NormalizeLookupValue(userEmail);

        if (string.IsNullOrWhiteSpace(normalizedUserCode) && string.IsNullOrWhiteSpace(normalizedUserEmail))
        {
            return items;
        }

        return items.Where(item =>
        {
            var entryUserCode = NormalizeLookupValue(item.UserCode);
            var entryUserEmail = NormalizeLookupValue(item.UserEmail);

            var matchesUserCode =
                !string.IsNullOrWhiteSpace(normalizedUserCode) &&
                (string.Equals(entryUserCode, normalizedUserCode, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(entryUserEmail, normalizedUserCode, StringComparison.OrdinalIgnoreCase));

            var matchesUserEmail =
                !string.IsNullOrWhiteSpace(normalizedUserEmail) &&
                (string.Equals(entryUserEmail, normalizedUserEmail, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(entryUserCode, normalizedUserEmail, StringComparison.OrdinalIgnoreCase));

            return matchesUserCode || matchesUserEmail;
        });
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

    private ListeningHistoryEntryDto NormalizeCreateRequest(
        ListeningHistoryCreateRequest request,
        PoiDto? poi)
    {
        var language = NormalizeLanguage(request.Language);

        return new ListeningHistoryEntryDto
        {
            Id = Guid.NewGuid(),
            PoiId = poi?.Id ?? request.PoiId,
            PoiCode = ResolvePoiValue(request.PoiCode, poi?.Code),
            PoiName = ResolvePoiValue(request.PoiName, poi?.Name),
            PoiAddress = ResolvePoiValue(request.PoiAddress, poi?.Address),
            PoiDescription = ResolvePoiValue(request.PoiDescription, poi?.Description),
            PoiSpecialDish = ResolvePoiValue(request.PoiSpecialDish, poi?.SpecialDish),
            PoiImageSource = ResolvePoiValue(request.PoiImageSource, poi?.ImageSource),
            PoiMapLink = ResolvePoiUrl(request.PoiMapLink, poi?.MapLink),
            UserCode = RepairText(request.UserCode, "guest"),
            UserDisplayName = RepairText(request.UserDisplayName, "Khách"),
            UserEmail = NormalizeEmail(request.UserEmail),
            TriggerType = RepairText(request.TriggerType, "APP"),
            Language = language,
            PlaybackMode = NormalizePlaybackMode(request.PlaybackMode),
            NarrationSnapshot = ResolveNarrationSnapshot(request.NarrationSnapshot, poi, language),
            AudioAssetPath = ResolvePoiUrl(request.AudioAssetPath, poi?.AudioAssetPath),
            Source = RepairText(request.Source, "app"),
            DevicePlatform = RepairText(request.DevicePlatform),
            AutoTriggered = request.AutoTriggered,
            StartedAtUtc = request.StartedAtUtc == default
                ? DateTimeOffset.UtcNow
                : request.StartedAtUtc.ToUniversalTime(),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = null,
            ListenSeconds = 0,
            Completed = false,
            ErrorMessage = string.Empty
        };
    }

    private ListeningHistoryEntryDto NormalizeStoredEntry(
        ListeningHistoryEntryDto item,
        PoiDto? poi)
    {
        var normalized = item.Clone();
        normalized.Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id;
        normalized.PoiId = poi?.Id ?? normalized.PoiId;
        normalized.PoiCode = ResolvePoiValue(normalized.PoiCode, poi?.Code);
        normalized.PoiName = ResolvePoiValue(normalized.PoiName, poi?.Name);
        normalized.PoiAddress = ResolvePoiValue(normalized.PoiAddress, poi?.Address);
        normalized.PoiDescription = ResolvePoiValue(normalized.PoiDescription, poi?.Description);
        normalized.PoiSpecialDish = ResolvePoiValue(normalized.PoiSpecialDish, poi?.SpecialDish);
        normalized.PoiImageSource = ResolvePoiValue(normalized.PoiImageSource, poi?.ImageSource);
        normalized.PoiMapLink = ResolvePoiUrl(normalized.PoiMapLink, poi?.MapLink);
        normalized.UserCode = RepairText(normalized.UserCode, "guest");
        normalized.UserDisplayName = RepairText(normalized.UserDisplayName, "Khách");
        normalized.UserEmail = NormalizeEmail(normalized.UserEmail);
        normalized.TriggerType = RepairText(normalized.TriggerType, "APP");
        normalized.Language = NormalizeLanguage(normalized.Language);
        normalized.PlaybackMode = NormalizePlaybackMode(normalized.PlaybackMode);
        normalized.NarrationSnapshot = ResolveNarrationSnapshot(normalized.NarrationSnapshot, poi, normalized.Language);
        normalized.AudioAssetPath = ResolvePoiUrl(normalized.AudioAssetPath, poi?.AudioAssetPath);
        normalized.Source = RepairText(normalized.Source, "app");
        normalized.DevicePlatform = RepairText(normalized.DevicePlatform);
        normalized.ErrorMessage = RepairText(normalized.ErrorMessage);
        normalized.ListenSeconds = Math.Max(0, normalized.ListenSeconds);
        normalized.StartedAtUtc = normalized.StartedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.StartedAtUtc.ToUniversalTime();
        normalized.ReceivedAtUtc = normalized.ReceivedAtUtc == default
            ? normalized.StartedAtUtc
            : normalized.ReceivedAtUtc.ToUniversalTime();
        normalized.CompletedAtUtc = normalized.CompletedAtUtc?.ToUniversalTime();
        normalized.TtsQueuePosition = Math.Max(1, normalized.TtsQueuePosition);
        return normalized;
    }

    private PoiDto? ResolvePoi(Guid poiId, string? poiCode, string? poiName)
    {
        if (poiId != Guid.Empty)
        {
            var poiById = _poiRepository.GetById(poiId);
            if (poiById is not null)
            {
                return poiById;
            }
        }

        var allPois = _poiRepository.GetAll();
        var normalizedPoiCode = NormalizeLookupValue(poiCode);
        if (!string.IsNullOrWhiteSpace(normalizedPoiCode))
        {
            var poiByCode = allPois.FirstOrDefault(poi =>
                string.Equals(NormalizeLookupValue(poi.Code), normalizedPoiCode, StringComparison.OrdinalIgnoreCase));
            if (poiByCode is not null)
            {
                return poiByCode;
            }
        }

        var normalizedPoiName = NormalizeLookupValue(LegacyTextRepair.Clean(poiName));
        return string.IsNullOrWhiteSpace(normalizedPoiName)
            ? null
            : allPois.FirstOrDefault(poi =>
                string.Equals(NormalizeLookupValue(poi.Name), normalizedPoiName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolvePoiValue(string? currentValue, string? poiValue)
    {
        var repaired = LegacyTextRepair.Clean(currentValue);
        if (!LegacyTextRepair.NeedsSeedFallback(repaired))
        {
            return repaired;
        }

        return LegacyTextRepair.Clean(poiValue);
    }

    private static string ResolvePoiUrl(string? currentValue, string? poiValue)
    {
        var repaired = LegacyTextRepair.Clean(currentValue);
        if (!string.IsNullOrWhiteSpace(repaired))
        {
            return repaired;
        }

        return LegacyTextRepair.Clean(poiValue);
    }

    private static string ResolveNarrationSnapshot(string? snapshot, PoiDto? poi, string language)
    {
        var repaired = LegacyTextRepair.Clean(snapshot);
        if (!LegacyTextRepair.NeedsSeedFallback(repaired))
        {
            return repaired;
        }

        if (poi is null)
        {
            return repaired;
        }

        if (poi.NarrationTranslations.TryGetValue(language, out var localizedNarration) &&
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

    private static string RepairText(string? value, string fallback = "")
    {
        var repaired = LegacyTextRepair.Clean(value);
        return LegacyTextRepair.NeedsSeedFallback(repaired)
            ? fallback
            : repaired;
    }

    private void RecalculateQueuePositionsUnsafe(Guid poiId)
    {
        if (poiId == Guid.Empty)
        {
            return;
        }

        var ordered = _items
            .Where(item => item.PoiId == poiId)
            .OrderBy(item => item.StartedAtUtc)
            .ThenBy(item => item.ReceivedAtUtc)
            .ThenBy(item => item.Id)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].TtsQueuePosition = index + 1;
        }
    }

    private static void RecalculateQueuePositions(List<ListeningHistoryEntryDto> items)
    {
        foreach (var group in items
            .Where(item => item.PoiId != Guid.Empty)
            .GroupBy(item => item.PoiId))
        {
            var ordered = group
                .OrderBy(item => item.StartedAtUtc)
                .ThenBy(item => item.ReceivedAtUtc)
                .ThenBy(item => item.Id)
                .ToList();

            for (var index = 0; index < ordered.Count; index++)
            {
                ordered[index].TtsQueuePosition = index + 1;
            }
        }
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

    private static string NormalizePlaybackMode(string? playbackMode)
    {
        return playbackMode?.Trim().ToLowerInvariant() switch
        {
            "audio" => "audio",
            _ => "tts"
        };
    }

    private static string NormalizeLanguage(string? language)
    {
        var normalized = LegacyTextRepair.Clean(language);
        return string.IsNullOrWhiteSpace(normalized) ? "vi" : normalized.Trim();
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeLookupValue(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}

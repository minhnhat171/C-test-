using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class PoiAdminService
{
    private readonly PoiApiClient _poiApiClient;
    private readonly AudioGuideApiClient _audioGuideApiClient;
    private readonly ListeningHistoryApiClient _listeningHistoryApiClient;
    private readonly ActiveDeviceApiClient _activeDeviceApiClient;

    public PoiAdminService(
        PoiApiClient poiApiClient,
        AudioGuideApiClient audioGuideApiClient,
        ListeningHistoryApiClient listeningHistoryApiClient,
        ActiveDeviceApiClient activeDeviceApiClient)
    {
        _poiApiClient = poiApiClient;
        _audioGuideApiClient = audioGuideApiClient;
        _listeningHistoryApiClient = listeningHistoryApiClient;
        _activeDeviceApiClient = activeDeviceApiClient;
    }

    public async Task<PoiManagementViewModel> LoadManagementPageAsync(
        string? searchTerm,
        string statusFilter,
        CancellationToken cancellationToken = default)
    {
        var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
        var audioGuidesTask = _audioGuideApiClient.GetAudioGuidesAsync(cancellationToken);

        await Task.WhenAll(poisTask, audioGuidesTask);

        var audioCountByPoi = audioGuidesTask.Result
            .GroupBy(item => item.PoiId)
            .ToDictionary(group => group.Key, group => group.Count());

        var query = poisTask.Result
            .Select(x =>
            {
                var item = x.ToListItem();
                item.RelatedAudioCount = audioCountByPoi.TryGetValue(x.Id, out var count) ? count : 0;
                return item;
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                x.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        query = statusFilter switch
        {
            "active" => query.Where(x => x.IsActive),
            "inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return new PoiManagementViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            StatusFilter = statusFilter,
            Items = query
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Name)
                .ToList()
        };
    }

    public async Task<MapPoiManagementViewModel> LoadMapManagementPageAsync(
        Guid? poiId,
        CancellationToken cancellationToken = default)
    {
        var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
        var audioGuidesTask = _audioGuideApiClient.GetAudioGuidesAsync(cancellationToken);

        await Task.WhenAll(poisTask, audioGuidesTask);

        var audioCountByPoi = audioGuidesTask.Result
            .GroupBy(item => item.PoiId)
            .ToDictionary(group => group.Key, group => group.Count());

        var orderedPois = poisTask.Result
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();

        var items = orderedPois
            .Select(x =>
            {
                var item = x.ToListItem();
                item.RelatedAudioCount = audioCountByPoi.TryGetValue(x.Id, out var count) ? count : 0;
                return item;
            })
            .ToList();

        var selectedPoi = orderedPois.FirstOrDefault(x => x.Id == poiId) ?? orderedPois.FirstOrDefault();
        var relatedAudioCount = selectedPoi is not null && audioCountByPoi.TryGetValue(selectedPoi.Id, out var count)
            ? count
            : 0;

        var vm = new MapPoiManagementViewModel
        {
            Pois = items,
            SelectedPoiId = selectedPoi?.Id ?? Guid.Empty,
            Editor = selectedPoi?.ToEditorViewModel(relatedAudioCount) ?? new PoiEditorViewModel()
        };

        try
        {
            var historyTask = _listeningHistoryApiClient.GetListeningHistoryAsync(
                sortBy: "time_desc",
                period: "all",
                cancellationToken: cancellationToken);
            var activeDevicesTask = _activeDeviceApiClient.GetStatsAsync(cancellationToken);

            await Task.WhenAll(historyTask, activeDevicesTask);
            ApplyMapAnalytics(vm, historyTask.Result, activeDevicesTask.Result);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            vm.AnalyticsLoadErrorMessage = "Khong the tai du lieu analytics tu VKFoodAPI. Map van cho phep chinh sua POI.";
        }

        return vm;
    }

    public async Task<IReadOnlyList<PoiListItemViewModel>> LoadValidationSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _poiApiClient.GetPoisAsync(cancellationToken))
            .Select(x => x.ToListItem())
            .ToList();
    }

    public async Task<PoiEditorViewModel?> LoadEditorAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var poiTask = _poiApiClient.GetPoiAsync(id, cancellationToken);
        var audioGuidesTask = _audioGuideApiClient.GetAudioGuidesAsync(cancellationToken);

        await Task.WhenAll(poiTask, audioGuidesTask);

        var poi = poiTask.Result;
        if (poi is null)
        {
            return null;
        }

        var relatedAudioCount = audioGuidesTask.Result.Count(item => item.PoiId == poi.Id);

        return poi.ToEditorViewModel(relatedAudioCount);
    }

    public async Task<PoiOperationResult> CreateAsync(
        PoiEditorViewModel model,
        CancellationToken cancellationToken = default)
    {
        var dto = new PoiDto { Id = Guid.NewGuid() };
        dto.ApplyEditorValues(model);

        var created = await _poiApiClient.CreatePoiAsync(dto, cancellationToken);

        return PoiOperationResult.Success(
            "Đã thêm POI mới. Danh sách, bản đồ và QR sẽ dùng dữ liệu mới từ API.",
            created.Id);
    }

    public async Task<PoiOperationResult> UpdateAsync(
        PoiEditorViewModel model,
        CancellationToken cancellationToken = default)
    {
        var poi = await _poiApiClient.GetPoiAsync(model.Id, cancellationToken);
        if (poi is null)
        {
            return PoiOperationResult.Missing("POI không còn tồn tại trên API.");
        }

        poi.ApplyEditorValues(model);
        var updated = await _poiApiClient.UpdatePoiAsync(poi.Id, poi, cancellationToken);

        return updated
            ? PoiOperationResult.Success(
                "Đã cập nhật POI. Danh sách, bản đồ và QR sẽ đọc dữ liệu mới từ API.",
                poi.Id)
            : PoiOperationResult.Missing("POI không còn tồn tại trên API.");
    }

    public async Task<PoiOperationResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return PoiOperationResult.Failure("Không xác định được POI cần xóa.");
        }

        var deleted = await _poiApiClient.DeletePoiAsync(id, cancellationToken);

        return deleted
            ? PoiOperationResult.Success("Đã xóa POI khỏi nguồn dữ liệu dùng chung.", id)
            : PoiOperationResult.Missing("Không tìm thấy POI để xóa.");
    }

    private static void ApplyMapAnalytics(
        MapPoiManagementViewModel vm,
        IReadOnlyList<ListeningHistoryEntryDto> history,
        ActiveDeviceStatsDto activeDevices)
    {
        var orderedHistory = history
            .OrderByDescending(item => item.StartedAtUtc)
            .ToList();

        vm.ActiveDeviceStats = activeDevices.Clone();
        vm.TotalListenSessions = orderedHistory.Count;
        vm.AverageListenSeconds = orderedHistory.Count == 0
            ? 0
            : orderedHistory.Average(item => item.ListenSeconds);
        vm.TopListeningPois = orderedHistory
            .GroupBy(item => new { item.PoiId, item.PoiCode, PoiName = ResolvePoiName(item) })
            .Select(group => new PoiListeningRankingItemViewModel
            {
                PoiId = group.Key.PoiId,
                PoiCode = group.Key.PoiCode,
                PoiName = group.Key.PoiName,
                ListenCount = group.Count(),
                CompletedCount = group.Count(item => item.Completed),
                TotalListenSeconds = group.Sum(item => item.ListenSeconds),
                LastStartedAtUtc = group.Max(item => (DateTimeOffset?)item.StartedAtUtc)
            })
            .OrderByDescending(item => item.ListenCount)
            .ThenByDescending(item => item.TotalListenSeconds)
            .ThenBy(item => item.PoiName)
            .Take(5)
            .ToList();
    }

    private static string ResolvePoiName(ListeningHistoryEntryDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.PoiName))
        {
            return item.PoiName;
        }

        if (!string.IsNullOrWhiteSpace(item.PoiCode))
        {
            return item.PoiCode;
        }

        return "POI khong xac dinh";
    }
}

public sealed record PoiOperationResult(
    bool Succeeded,
    bool NotFound,
    string Message,
    Guid? PoiId)
{
    public static PoiOperationResult Success(string message, Guid poiId)
        => new(true, false, message, poiId);

    public static PoiOperationResult Missing(string message)
        => new(false, true, message, null);

    public static PoiOperationResult Failure(string message)
        => new(false, false, message, null);
}

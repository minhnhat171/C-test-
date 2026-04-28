using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class PoiAdminService
{
    private readonly PoiApiClient _poiApiClient;
    private readonly AudioGuideApiClient _audioGuideApiClient;
    private readonly ListeningHistoryApiClient _listeningHistoryApiClient;
    private readonly ActiveDeviceApiClient _activeDeviceApiClient;
    private readonly IWebAdminCurrentUser _currentUser;
    private readonly PoiImageStorageService _imageStorage;

    public PoiAdminService(
        PoiApiClient poiApiClient,
        AudioGuideApiClient audioGuideApiClient,
        ListeningHistoryApiClient listeningHistoryApiClient,
        ActiveDeviceApiClient activeDeviceApiClient,
        IWebAdminCurrentUser currentUser,
        PoiImageStorageService imageStorage)
    {
        _poiApiClient = poiApiClient;
        _audioGuideApiClient = audioGuideApiClient;
        _listeningHistoryApiClient = listeningHistoryApiClient;
        _activeDeviceApiClient = activeDeviceApiClient;
        _currentUser = currentUser;
        _imageStorage = imageStorage;
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

        var query = FilterPoisForCurrentUser(poisTask.Result)
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

        var normalizedStatusFilter = NormalizeStatusFilter(statusFilter);
        query = normalizedStatusFilter switch
        {
            "active" => query.Where(x => x.IsActive),
            "inactive" => query.Where(x => !x.IsActive && !x.IsPendingApproval),
            "pending" => query.Where(x => x.IsPendingApproval),
            _ => query
        };

        return new PoiManagementViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            StatusFilter = normalizedStatusFilter,
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

        var orderedPois = FilterPoisForCurrentUser(poisTask.Result)
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
            vm.AnalyticsLoadErrorMessage = "Không thể tải dữ liệu analytics từ VKFoodAPI. Bản đồ sẽ hiển thị lại heatmap và tuyến di chuyển khi API sẵn sàng.";
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
        if (poi is null || !_currentUser.CanManage(poi))
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
        var imageResult = await _imageStorage.SaveAsync(model.UploadedImage, cancellationToken);
        if (!imageResult.Succeeded)
        {
            return PoiOperationResult.Failure(imageResult.ErrorMessage ?? "Không lưu được ảnh POI.");
        }

        if (!string.IsNullOrWhiteSpace(imageResult.PublicPath))
        {
            model.ImageSource = imageResult.PublicPath;
        }

        var dto = new PoiDto { Id = Guid.NewGuid() };
        dto.ApplyEditorValues(model);
        var isOwnerSubmission = _currentUser.IsPoiOwner && !_currentUser.IsAdmin;
        if (_currentUser.IsPoiOwner && !_currentUser.IsAdmin)
        {
            ApplyOwner(dto);
            dto.IsActive = false;
        }

        var created = await _poiApiClient.CreatePoiAsync(dto, cancellationToken);

        return PoiOperationResult.Success(
            isOwnerSubmission
                ? "Đã gửi POI cho Admin duyệt. POI sẽ hoạt động trên app sau khi được duyệt."
                : "Đã thêm POI mới. Danh sách, bản đồ và QR sẽ dùng dữ liệu mới từ API.",
            created.Id);
    }

    public async Task<PoiOperationResult> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            return PoiOperationResult.Missing("Bạn không có quyền duyệt POI.");
        }

        if (id == Guid.Empty)
        {
            return PoiOperationResult.Failure("Không xác định được POI cần duyệt.");
        }

        var poi = await _poiApiClient.GetPoiAsync(id, cancellationToken);
        if (poi is null)
        {
            return PoiOperationResult.Missing("Không tìm thấy POI cần duyệt.");
        }

        if (poi.IsActive)
        {
            return PoiOperationResult.Success("POI đã ở trạng thái hoạt động.", poi.Id);
        }

        poi.IsActive = true;
        var updated = await _poiApiClient.UpdatePoiAsync(poi.Id, poi, cancellationToken);

        return updated
            ? PoiOperationResult.Success("Đã duyệt POI. POI hiện đã hoạt động trên web và app.", poi.Id)
            : PoiOperationResult.Missing("Không tìm thấy POI cần duyệt.");
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

        if (!_currentUser.CanManage(poi))
        {
            return PoiOperationResult.Missing("Bạn không có quyền cập nhật POI này.");
        }

        var imageResult = await _imageStorage.SaveAsync(model.UploadedImage, cancellationToken);
        if (!imageResult.Succeeded)
        {
            return PoiOperationResult.Failure(imageResult.ErrorMessage ?? "Không lưu được ảnh POI.");
        }

        if (!string.IsNullOrWhiteSpace(imageResult.PublicPath))
        {
            model.ImageSource = imageResult.PublicPath;
        }

        poi.ApplyEditorValues(model);
        if (_currentUser.IsPoiOwner && !_currentUser.IsAdmin)
        {
            ApplyOwner(poi);
        }
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

        var existing = await _poiApiClient.GetPoiAsync(id, cancellationToken);
        if (existing is null)
        {
            return PoiOperationResult.Missing("Không tìm thấy POI để xóa.");
        }

        if (!_currentUser.CanManage(existing))
        {
            return PoiOperationResult.Missing("Bạn không có quyền xóa POI này.");
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
        var routePoints = activeDevices.RoutePoints
            .Where(point => HasValidLocation(point.Latitude, point.Longitude))
            .OrderBy(point => point.RecordedAtUtc)
            .ToList();
        var locatedDevices = activeDevices.Devices
            .Where(device => HasValidLocation(device.Latitude, device.Longitude))
            .ToList();
        vm.Routes = BuildMapRoutes(routePoints)
            .OrderByDescending(route => route.EndedAtUtc)
            .ThenByDescending(route => route.PointCount)
            .Take(12)
            .ToList();
        vm.HeatmapPoints = BuildHeatmapPoints(routePoints, locatedDevices, maxHeatPoints: 300);
        vm.AnalyzedMovementPointCount = routePoints.Count;
        vm.AnonymousRouteCount = vm.Routes.Count;
        vm.AnonymousVisitorCount = routePoints.Count > 0
            ? routePoints
                .Select(point => point.AnonymousRouteId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
            : locatedDevices.Count;
        vm.LatestMovementAtUtc = routePoints.Count > 0
            ? routePoints.Max(point => (DateTimeOffset?)point.RecordedAtUtc)
            : locatedDevices
                .Select(device => (DateTimeOffset?)(device.LocationTimestampUtc ?? device.LastSeenAtUtc))
                .DefaultIfEmpty(null)
                .Max();
        vm.AnalyticsWindowLabel = BuildAnalyticsWindowLabel(routePoints.Count, locatedDevices.Count);
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

    private static List<MapRouteViewModel> BuildMapRoutes(IReadOnlyList<ActiveDeviceRoutePointDto> routePoints)
    {
        var routes = new List<MapRouteViewModel>();
        var routeIndex = 1;

        foreach (var group in routePoints
                     .Where(point => !string.IsNullOrWhiteSpace(point.AnonymousRouteId))
                     .GroupBy(point => point.AnonymousRouteId, StringComparer.OrdinalIgnoreCase))
        {
            var points = group
                .OrderBy(point => point.RecordedAtUtc)
                .ToList();

            if (points.Count < 2)
            {
                continue;
            }

            var totalDistanceMeters = 0d;
            for (var index = 1; index < points.Count; index++)
            {
                totalDistanceMeters += CalculateDistanceMeters(
                    points[index - 1].Latitude,
                    points[index - 1].Longitude,
                    points[index].Latitude,
                    points[index].Longitude);
            }

            var firstPoint = points[0];
            var lastPoint = points[^1];
            routes.Add(new MapRouteViewModel
            {
                RouteId = group.Key,
                RouteLabel = $"Thiết bị {routeIndex}",
                PointCount = points.Count,
                StartedAtUtc = firstPoint.RecordedAtUtc,
                EndedAtUtc = lastPoint.RecordedAtUtc,
                DurationMinutes = Math.Max((lastPoint.RecordedAtUtc - firstPoint.RecordedAtUtc).TotalMinutes, 0),
                ApproxDistanceMeters = totalDistanceMeters,
                Points = points
                    .Select(point => new MapGeoPointViewModel
                    {
                        Latitude = point.Latitude,
                        Longitude = point.Longitude,
                        RecordedAtUtc = point.RecordedAtUtc,
                        AccuracyMeters = point.AccuracyMeters
                    })
                    .ToList()
            });

            routeIndex += 1;
        }

        return routes;
    }

    private static List<MapHeatPointViewModel> BuildHeatmapPoints(
        IReadOnlyList<ActiveDeviceRoutePointDto> routePoints,
        IReadOnlyList<ActiveDeviceSessionDto> locatedDevices,
        int maxHeatPoints)
    {
        var samples = routePoints.Count > 0
            ? routePoints.Select(point => new
            {
                point.Latitude,
                point.Longitude
            })
            : locatedDevices.Select(device => new
            {
                Latitude = device.Latitude!.Value,
                Longitude = device.Longitude!.Value
            });

        var buckets = samples
            .GroupBy(point => new
            {
                Latitude = Math.Round(point.Latitude, 4),
                Longitude = Math.Round(point.Longitude, 4)
            })
            .Select(group => new
            {
                Latitude = group.Average(point => point.Latitude),
                Longitude = group.Average(point => point.Longitude),
                Count = group.Count()
            })
            .OrderByDescending(point => point.Count)
            .Take(Math.Clamp(maxHeatPoints, 50, 500))
            .ToList();

        var maxCount = buckets.Count == 0 ? 1 : buckets.Max(point => point.Count);

        return buckets
            .Select(point => new MapHeatPointViewModel
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Count = point.Count,
                Weight = maxCount <= 0
                    ? 0
                    : (double)point.Count / maxCount
            })
            .ToList();
    }

    private static string BuildAnalyticsWindowLabel(int routePointCount, int locatedDeviceCount)
    {
        if (routePointCount > 0)
        {
            return $"Phân tích {routePointCount:N0} điểm tuyến trong 12 giờ gần nhất.";
        }

        if (locatedDeviceCount > 0)
        {
            return $"Dùng {locatedDeviceCount:N0} vị trí online hiện tại để vẽ heatmap.";
        }

        return "Chưa có dữ liệu di chuyển.";
    }

    private static bool HasValidLocation(double? latitude, double? longitude)
    {
        return latitude.HasValue &&
               longitude.HasValue &&
               HasValidLocation(latitude.Value, longitude.Value);
    }

    private static bool HasValidLocation(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180 &&
               (Math.Abs(latitude) > 0.000001 || Math.Abs(longitude) > 0.000001);
    }

    private static double CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadiusMeters = 6371000;
        var latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        var longitudeDelta = DegreesToRadians(longitude2 - longitude1);
        var startLatitude = DegreesToRadians(latitude1);
        var endLatitude = DegreesToRadians(latitude2);

        var a =
            Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2) +
            Math.Cos(startLatitude) * Math.Cos(endLatitude) *
            Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double value)
        => value * Math.PI / 180.0;

    private IReadOnlyList<PoiDto> FilterPoisForCurrentUser(IReadOnlyList<PoiDto> pois)
    {
        if (_currentUser.IsAdmin)
        {
            return pois;
        }

        if (!_currentUser.IsPoiOwner)
        {
            return [];
        }

        return pois
            .Where(_currentUser.CanManage)
            .ToList();
    }

    private void ApplyOwner(PoiDto dto)
    {
        dto.OwnerUserCode = _currentUser.OwnerCode;
        dto.OwnerDisplayName = _currentUser.DisplayName;
        dto.OwnerEmail = _currentUser.OwnerEmail;
    }

    private static string NormalizeStatusFilter(string? statusFilter)
    {
        return statusFilter?.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "inactive" => "inactive",
            "pending" => "pending",
            _ => "all"
        };
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

        return "POI không xác định";
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

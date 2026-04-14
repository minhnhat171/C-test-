using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class TourAdminService
{
    private readonly TourApiClient _tourApiClient;
    private readonly PoiApiClient _poiApiClient;

    public TourAdminService(
        TourApiClient tourApiClient,
        PoiApiClient poiApiClient)
    {
        _tourApiClient = tourApiClient;
        _poiApiClient = poiApiClient;
    }

    public async Task<TourManagementViewModel> LoadManagementPageAsync(
        string? searchTerm,
        string statusFilter,
        CancellationToken cancellationToken = default)
    {
        var poisTask = LoadAvailablePoisAsync(cancellationToken);
        var toursTask = _tourApiClient.GetToursAsync(cancellationToken);

        await Task.WhenAll(poisTask, toursTask);

        var poiLookup = poisTask.Result.ToDictionary(item => item.Id);
        var query = toursTask.Result
            .Select(item => item.ToListItem(poiLookup))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(item =>
                item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.StopNames.Any(stop => stop.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        query = statusFilter switch
        {
            "active" => query.Where(item => item.IsActive),
            "inactive" => query.Where(item => !item.IsActive),
            _ => query
        };

        return new TourManagementViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            StatusFilter = statusFilter,
            Items = query
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToList()
        };
    }

    public async Task<TourEditorViewModel> BuildCreateEditorAsync(
        CancellationToken cancellationToken = default)
    {
        var availablePois = await LoadAvailablePoisAsync(cancellationToken);

        return new TourEditorViewModel
        {
            AvailablePois = availablePois.ToList()
        };
    }

    public async Task<TourEditorViewModel?> LoadEditorAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var tourTask = _tourApiClient.GetTourAsync(id, cancellationToken);
        var poisTask = LoadAvailablePoisAsync(cancellationToken);

        await Task.WhenAll(tourTask, poisTask);

        var tour = tourTask.Result;
        if (tour is null)
        {
            return null;
        }

        return tour.ToEditorViewModel(poisTask.Result);
    }

    public async Task PopulateEditorReferencesAsync(
        TourEditorViewModel model,
        CancellationToken cancellationToken = default)
    {
        var availablePois = await LoadAvailablePoisAsync(cancellationToken);
        model.ApplyReferenceData(availablePois);
    }

    public async Task<TourOperationResult> CreateAsync(
        TourEditorViewModel model,
        CancellationToken cancellationToken = default)
    {
        var dto = new TourDto();
        dto.ApplyEditorValues(model);

        var created = await _tourApiClient.CreateTourAsync(dto, cancellationToken);

        return TourOperationResult.Success(
            "Đã tạo tour mới. App MAUI sẽ nhận tour này từ API trong lần đồng bộ kế tiếp.",
            created.Id);
    }

    public async Task<TourOperationResult> UpdateAsync(
        TourEditorViewModel model,
        CancellationToken cancellationToken = default)
    {
        var tour = await _tourApiClient.GetTourAsync(model.Id, cancellationToken);
        if (tour is null)
        {
            return TourOperationResult.Missing("Tour không còn tồn tại trên API.");
        }

        tour.ApplyEditorValues(model);
        var updated = await _tourApiClient.UpdateTourAsync(tour.Id, tour, cancellationToken);

        return updated
            ? TourOperationResult.Success(
                "Đã cập nhật tour. App MAUI và trang QR sẽ đọc lại dữ liệu tour mới từ API.",
                tour.Id)
            : TourOperationResult.Missing("Tour không còn tồn tại trên API.");
    }

    public async Task<TourOperationResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return TourOperationResult.Failure("Không xác định được tour cần xóa.");
        }

        var deleted = await _tourApiClient.DeleteTourAsync(id, cancellationToken);

        return deleted
            ? TourOperationResult.Success("Đã xóa tour khỏi nguồn dữ liệu dùng chung.", id)
            : TourOperationResult.Missing("Không tìm thấy tour để xóa.");
    }

    public async Task<IReadOnlyList<PoiLookupItemViewModel>> LoadAvailablePoisAsync(
        CancellationToken cancellationToken = default)
    {
        return (await _poiApiClient.GetPoisAsync(cancellationToken))
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Name)
            .Select(item => item.ToLookupItem())
            .ToList();
    }
}

public sealed record TourOperationResult(
    bool Succeeded,
    bool NotFound,
    string Message,
    int? TourId)
{
    public static TourOperationResult Success(string message, int tourId)
        => new(true, false, message, tourId);

    public static TourOperationResult Missing(string message)
        => new(false, true, message, null);

    public static TourOperationResult Failure(string message)
        => new(false, false, message, null);
}

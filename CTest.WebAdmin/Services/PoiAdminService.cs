using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class PoiAdminService
{
    private readonly PoiApiClient _poiApiClient;
    private readonly AudioGuideApiClient _audioGuideApiClient;

    public PoiAdminService(
        PoiApiClient poiApiClient,
        AudioGuideApiClient audioGuideApiClient)
    {
        _poiApiClient = poiApiClient;
        _audioGuideApiClient = audioGuideApiClient;
    }

    public async Task<PoiManagementViewModel> LoadManagementPageAsync(
        string? searchTerm,
        string statusFilter,
        CancellationToken cancellationToken = default)
    {
        var query = (await _poiApiClient.GetPoisAsync(cancellationToken))
            .Select(x => x.ToListItem())
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

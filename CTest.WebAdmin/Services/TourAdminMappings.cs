using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

internal static class TourAdminMappings
{
    public static TourListItemViewModel ToListItem(
        this TourDto dto,
        IReadOnlyDictionary<Guid, PoiLookupItemViewModel> poiLookup)
    {
        var stopNames = ResolveStopNames(dto.PoiIds, poiLookup);

        return new TourListItemViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            EstimatedMinutes = dto.EstimatedMinutes,
            IsActive = dto.IsActive,
            IsQrEnabled = dto.IsQrEnabled,
            StopNames = stopNames
        };
    }

    public static TourEditorViewModel ToEditorViewModel(
        this TourDto dto,
        IReadOnlyList<PoiLookupItemViewModel> availablePois)
    {
        var poiLookup = availablePois.ToDictionary(item => item.Id);

        return new TourEditorViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            EstimatedMinutes = dto.EstimatedMinutes,
            IsActive = dto.IsActive,
            IsQrEnabled = dto.IsQrEnabled,
            IsEditMode = true,
            SelectedPoiIds = dto.PoiIds.ToList(),
            SelectedStops = dto.PoiIds
                .Select(poiId => BuildSelectedStop(poiId, poiLookup))
                .ToList(),
            AvailablePois = availablePois.ToList()
        };
    }

    public static void ApplyEditorValues(this TourDto dto, TourEditorViewModel model)
    {
        dto.Code = model.Code?.Trim() ?? string.Empty;
        dto.Name = model.Name?.Trim() ?? string.Empty;
        dto.Description = model.Description?.Trim() ?? string.Empty;
        dto.EstimatedMinutes = model.EstimatedMinutes <= 0 ? 45 : model.EstimatedMinutes;
        dto.IsActive = model.IsActive;
        dto.IsQrEnabled = model.IsQrEnabled;
        dto.PoiIds = (model.SelectedPoiIds ?? [])
            .Where(poiId => poiId != Guid.Empty)
            .Distinct()
            .ToList();
    }

    public static void ApplyReferenceData(
        this TourEditorViewModel model,
        IReadOnlyList<PoiLookupItemViewModel> availablePois)
    {
        var poiLookup = availablePois.ToDictionary(item => item.Id);

        model.AvailablePois = availablePois.ToList();
        model.SelectedStops = (model.SelectedPoiIds ?? [])
            .Where(poiId => poiId != Guid.Empty)
            .Select(poiId => BuildSelectedStop(poiId, poiLookup))
            .ToList();
    }

    private static List<string> ResolveStopNames(
        IEnumerable<Guid> poiIds,
        IReadOnlyDictionary<Guid, PoiLookupItemViewModel> poiLookup)
    {
        return poiIds
            .Where(poiId => poiId != Guid.Empty)
            .Select(poiId => poiLookup.TryGetValue(poiId, out var poi)
                ? poi.Name
                : $"POI {poiId.ToString("N")[..6].ToUpperInvariant()}")
            .ToList();
    }

    private static TourEditorStopViewModel BuildSelectedStop(
        Guid poiId,
        IReadOnlyDictionary<Guid, PoiLookupItemViewModel> poiLookup)
    {
        if (poiLookup.TryGetValue(poiId, out var poi))
        {
            return new TourEditorStopViewModel
            {
                PoiId = poiId,
                Name = poi.Name,
                Address = poi.Address,
                IsActive = poi.IsActive
            };
        }

        return new TourEditorStopViewModel
        {
            PoiId = poiId,
            Name = $"POI {poiId.ToString("N")[..6].ToUpperInvariant()}",
            Address = "POI này không còn tồn tại trên API.",
            IsActive = false
        };
    }
}

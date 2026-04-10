using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

public class PoisController : Controller
{
    private readonly PoiApiClient _poiApiClient;
    private readonly AppDataService _data;

    public PoisController(PoiApiClient poiApiClient, AppDataService data)
    {
        _poiApiClient = poiApiClient;
        _data = data;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string statusFilter = "all",
        CancellationToken cancellationToken = default)
    {
        var vm = new PoiManagementViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            StatusFilter = statusFilter
        };

        try
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

            vm.Items = query
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Name)
                .ToList();
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Khong the ket noi VKFoodAPI. Hay chay API truoc de WebAdmin va app MAUI dung chung du lieu.";
        }

        return View("Manage", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await LoadEditorViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["PoiMessage"] = "Khong tim thay POI can xem.";
            return RedirectToAction(nameof(Index));
        }

        return View("Details", model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Editor", new PoiEditorViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await LoadEditorViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["PoiMessage"] = "Khong tim thay POI can sua.";
            return RedirectToAction(nameof(Index));
        }

        return View("Editor", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PoiEditorViewModel model)
    {
        var poi = new Poi
        {
            Id = _data.Pois.Any() ? _data.Pois.Max(x => x.Id) + 1 : 1,
            Name = model.Name,
            Address = model.Address,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            RadiusInMeters = model.RadiusInMeters,
            Priority = model.Priority,
            Description = model.Description,
            MapLink = model.MapLink,
            NarrationScript = model.NarrationScript,
            IsActive = model.IsActive
        };

        _data.Pois.Add(poi);
        TempData["PoiMessage"] = "Đã thêm POI mới.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(PoiEditorViewModel model)
    {
        var poi = _data.Pois.FirstOrDefault(x => x.Id == model.Id);
        if (poi is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var oldName = poi.Name;

        poi.Name = model.Name;
        poi.Address = model.Address;
        poi.Latitude = model.Latitude;
        poi.Longitude = model.Longitude;
        poi.RadiusInMeters = model.RadiusInMeters;
        poi.Priority = model.Priority;
        poi.Description = model.Description;
        poi.MapLink = model.MapLink;
        poi.NarrationScript = model.NarrationScript;
        poi.IsActive = model.IsActive;

        foreach (var audioGuide in _data.AudioGuides.Where(x => x.PoiId == model.Id))
        {
            audioGuide.PoiName = model.Name;
        }

        foreach (var translation in _data.Translations.Where(x => x.PoiId == model.Id))
        {
            translation.PoiName = model.Name;
        }

        if (!string.Equals(oldName, model.Name, StringComparison.Ordinal))
        {
            foreach (var usageLog in _data.UsageLogs.Where(x => x.PoiName == oldName))
            {
                usageLog.PoiName = model.Name;
            }

            foreach (var tour in _data.Tours.Where(x => x.PoiSequence.Contains(oldName, StringComparison.Ordinal)))
            {
                tour.PoiSequence = tour.PoiSequence.Replace(oldName, model.Name, StringComparison.Ordinal);
            }
        }

        TempData["PoiMessage"] = "Đã cập nhật POI.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var poi = _data.Pois.FirstOrDefault(x => x.Id == id);
        if (poi is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _data.Pois.Remove(poi);
        _data.AudioGuides.RemoveAll(x => x.PoiId == id);
        _data.Translations.RemoveAll(x => x.PoiId == id);

        TempData["PoiMessage"] = "Đã xóa POI.";
        return RedirectToAction(nameof(Index));
    }

    private PoiEditorViewModel BuildEditorViewModel(Poi poi, bool isEditMode)
    {
        return new PoiEditorViewModel
        {
            Id = poi.Id,
            Name = poi.Name,
            Address = poi.Address,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            RadiusInMeters = poi.RadiusInMeters,
            Priority = poi.Priority,
            Description = poi.Description,
            MapLink = poi.MapLink,
            NarrationScript = poi.NarrationScript,
            IsActive = poi.IsActive,
            IsEditMode = isEditMode,
            RelatedAudioCount = _data.AudioGuides.Count(x => x.PoiId == poi.Id),
            RelatedTranslationCount = _data.Translations.Count(x => x.PoiId == poi.Id),
            IsQrEnabled = ContainsQr(poi)
        };
    }

    private static bool ContainsQr(Poi poi)
    {
        return poi.Description.Contains("QR", StringComparison.OrdinalIgnoreCase) ||
               poi.NarrationScript.Contains("QR", StringComparison.OrdinalIgnoreCase);
    }
}

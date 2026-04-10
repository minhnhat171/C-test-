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
    public async Task<IActionResult> Create(PoiEditorViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ValidateEditor(model))
        {
            return View("Editor", model);
        }

        try
        {
            var dto = new PoiDto { Id = Guid.NewGuid() };
            dto.ApplyEditorValues(model);

            await _poiApiClient.CreatePoiAsync(dto, cancellationToken);
            TempData["PoiMessage"] = "Da them POI moi va dong bo sang app MAUI.";

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Khong the ket noi VKFoodAPI. Hay mo API truoc roi luu lai.");
            return View("Editor", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiEditorViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ValidateEditor(model))
        {
            model.IsEditMode = true;
            return View("Editor", model);
        }

        try
        {
            var poi = await _poiApiClient.GetPoiAsync(model.Id, cancellationToken);
            if (poi is null)
            {
                TempData["PoiMessage"] = "POI khong con ton tai tren API.";
                return RedirectToAction(nameof(Index));
            }

            poi.ApplyEditorValues(model);
            await _poiApiClient.UpdatePoiAsync(poi.Id, poi, cancellationToken);

            TempData["PoiMessage"] = "Da cap nhat POI. App MAUI se doc du lieu moi tu API.";
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Khong the ket noi VKFoodAPI. Hay mo API truoc roi luu lai.");
            model.IsEditMode = true;
            return View("Editor", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _poiApiClient.DeletePoiAsync(id, cancellationToken);
            TempData["PoiMessage"] = deleted
                ? "Da xoa POI khoi nguon du lieu dung chung."
                : "Khong tim thay POI de xoa.";
        }
        catch (HttpRequestException)
        {
            TempData["PoiMessage"] = "Khong the ket noi VKFoodAPI nen chua xoa duoc POI.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<PoiEditorViewModel?> LoadEditorViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var poi = await _poiApiClient.GetPoiAsync(id, cancellationToken);
        if (poi is null)
        {
            return null;
        }

        var relatedAudioCount = _data.AudioGuides.Count(x =>
            string.Equals(x.PoiName, poi.Name, StringComparison.OrdinalIgnoreCase));

        return poi.ToEditorViewModel(relatedAudioCount);
    }

    private bool ValidateEditor(PoiEditorViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Ten diem khong duoc de trong.");
        }

        if (string.IsNullOrWhiteSpace(model.Address))
        {
            ModelState.AddModelError(nameof(model.Address), "Dia chi khong duoc de trong.");
        }

        if (string.IsNullOrWhiteSpace(model.NarrationScript))
        {
            ModelState.AddModelError(nameof(model.NarrationScript), "Script thuyet minh khong duoc de trong.");
        }

        return ModelState.IsValid;
    }
}

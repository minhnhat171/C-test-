using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

public class PoisController : Controller
{
    private readonly PoiAdminService _poiService;
    private readonly PoiValidationService _validationService;

    public PoisController(PoiAdminService poiService, PoiValidationService validationService)
    {
        _poiService = poiService;
        _validationService = validationService;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string statusFilter = "all",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vm = await _poiService.LoadManagementPageAsync(searchTerm, statusFilter, cancellationToken);
            return View("Manage", vm);
        }
        catch (HttpRequestException)
        {
            return View("Manage", new PoiManagementViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                StatusFilter = statusFilter,
                LoadErrorMessage = "Không thể kết nối VKFoodAPI. Hãy chạy API trước để WebAdmin và app MAUI dùng chung dữ liệu."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _poiService.LoadEditorAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["PoiMessage"] = "Không tìm thấy POI cần xem.";
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
        var model = await _poiService.LoadEditorAsync(id, cancellationToken);
        if (model is null)
        {
            TempData["PoiMessage"] = "Không tìm thấy POI cần sửa.";
            return RedirectToAction(nameof(Index));
        }

        return View("Editor", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiEditorViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingPois = await _poiService.LoadValidationSnapshotAsync(cancellationToken);
            var validation = _validationService.ValidateCreate(model, existingPois);

            if (!ApplyValidationResult(validation))
            {
                return View("Editor", model);
            }

            var result = await _poiService.CreateAsync(model, cancellationToken);
            TempData["PoiMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối VKFoodAPI. Hãy mở API trước rồi lưu lại.");
            return View("Editor", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiEditorViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingPois = await _poiService.LoadValidationSnapshotAsync(cancellationToken);
            var validation = _validationService.ValidateUpdate(model, existingPois);

            if (!ApplyValidationResult(validation))
            {
                model.IsEditMode = true;
                return View("Editor", model);
            }

            var result = await _poiService.UpdateAsync(model, cancellationToken);
            TempData["PoiMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối VKFoodAPI. Hãy mở API trước rồi lưu lại.");
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
            var result = await _poiService.DeleteAsync(id, cancellationToken);
            TempData["PoiMessage"] = result.Message;
        }
        catch (HttpRequestException)
        {
            TempData["PoiMessage"] = "Không thể kết nối VKFoodAPI nên chưa xóa được POI.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ApplyValidationResult(PoiValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }

        return validation.IsValid;
    }
}

using CTest.WebAdmin.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Services;
using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class ToursController : Controller
{
    private readonly TourAdminService _tourService;

    public ToursController(TourAdminService tourService)
    {
        _tourService = tourService;
    }

    public async Task<IActionResult> Index(
        string? searchTerm,
        string statusFilter = "all",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vm = await _tourService.LoadManagementPageAsync(searchTerm, statusFilter, cancellationToken);
            return View(vm);
        }
        catch (HttpRequestException)
        {
            return View(new TourManagementViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                StatusFilter = statusFilter,
                LoadErrorMessage = "Không thể kết nối VKFoodAPI. Phần quản lý tour chỉ hoạt động khi API đang chạy."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        try
        {
            var vm = await _tourService.BuildCreateEditorAsync(cancellationToken);
            return View("Editor", vm);
        }
        catch (HttpRequestException)
        {
            TempData["TourMessage"] = "Không thể kết nối VKFoodAPI để tải danh sách POI.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vm = await _tourService.LoadEditorAsync(id, cancellationToken);
            if (vm is null)
            {
                TempData["TourMessage"] = "Không tìm thấy tour cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            return View("Editor", vm);
        }
        catch (HttpRequestException)
        {
            TempData["TourMessage"] = "Không thể kết nối VKFoodAPI để tải tour.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TourEditorViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tourService.PopulateEditorReferencesAsync(model, cancellationToken);
            ValidateEditor(model);

            if (!ModelState.IsValid)
            {
                return View("Editor", model);
            }

            var result = await _tourService.CreateAsync(model, cancellationToken);
            TempData["TourMessage"] = result.Message;
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
    public async Task<IActionResult> Edit(TourEditorViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _tourService.PopulateEditorReferencesAsync(model, cancellationToken);
            model.IsEditMode = true;
            ValidateEditor(model);

            if (!ModelState.IsValid)
            {
                return View("Editor", model);
            }

            var result = await _tourService.UpdateAsync(model, cancellationToken);
            TempData["TourMessage"] = result.Message;
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _tourService.DeleteAsync(id, cancellationToken);
            TempData["TourMessage"] = result.Message;
        }
        catch (HttpRequestException)
        {
            TempData["TourMessage"] = "Không thể kết nối VKFoodAPI nên chưa xóa được tour.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateEditor(TourEditorViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(TourEditorViewModel.Name), "Hãy nhập tên tour.");
        }

        if (model.EstimatedMinutes <= 0)
        {
            ModelState.AddModelError(nameof(TourEditorViewModel.EstimatedMinutes), "Thời lượng phải lớn hơn 0.");
        }

        if (model.SelectedPoiIds.Count == 0)
        {
            ModelState.AddModelError(nameof(TourEditorViewModel.SelectedPoiIds), "Hãy thêm ít nhất một POI vào tour.");
        }

        if (model.SelectedPoiIds.Distinct().Count() != model.SelectedPoiIds.Count)
        {
            ModelState.AddModelError(nameof(TourEditorViewModel.SelectedPoiIds), "Danh sách tour đang có POI trùng lặp.");
        }

        var availablePoiIds = model.AvailablePois
            .Select(item => item.Id)
            .ToHashSet();
        var invalidPoiCount = model.SelectedPoiIds.Count(poiId => !availablePoiIds.Contains(poiId));
        if (invalidPoiCount > 0)
        {
            ModelState.AddModelError(nameof(TourEditorViewModel.SelectedPoiIds), "Một hoặc nhiều POI trong tour không còn tồn tại trên API.");
        }
    }
}

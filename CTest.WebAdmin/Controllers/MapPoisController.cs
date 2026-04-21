using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class MapPoisController : Controller
{
    private readonly PoiAdminService _poiService;
    private readonly PoiValidationService _validationService;

    public MapPoisController(
        PoiAdminService poiService,
        PoiValidationService validationService)
    {
        _poiService = poiService;
        _validationService = validationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? poiId, CancellationToken cancellationToken = default)
    {
        var vm = await BuildPageAsync(poiId, editorOverride: null, cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        MapPoiManagementViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Editor.IsEditMode = true;

        try
        {
            var existingPois = await _poiService.LoadValidationSnapshotAsync(cancellationToken);
            var validation = _validationService.ValidateUpdate(model.Editor, existingPois);

            if (!ApplyValidationResult(validation))
            {
                var invalidVm = await BuildPageAsync(model.Editor.Id, model.Editor, cancellationToken);
                return View("Index", invalidVm);
            }

            var result = await _poiService.UpdateAsync(model.Editor, cancellationToken);
            TempData["MapPoiMessage"] = result.Message;

            return RedirectToAction(nameof(Index), new { poiId = model.Editor.Id });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối VKFoodAPI. Hãy mở API trước rồi lưu lại.");

            var offlineVm = await BuildPageAsync(model.Editor.Id, model.Editor, cancellationToken);
            return View("Index", offlineVm);
        }
    }

    private async Task<MapPoiManagementViewModel> BuildPageAsync(
        Guid? poiId,
        PoiEditorViewModel? editorOverride,
        CancellationToken cancellationToken)
    {
        try
        {
            var vm = await _poiService.LoadMapManagementPageAsync(poiId, cancellationToken);
            if (editorOverride is null)
            {
                return vm;
            }

            editorOverride.RelatedAudioCount = vm.Editor.RelatedAudioCount;
            editorOverride.RelatedTranslationCount = vm.Editor.RelatedTranslationCount;
            editorOverride.IsQrEnabled = vm.Editor.IsQrEnabled;
            editorOverride.IsEditMode = true;

            vm.SelectedPoiId = editorOverride.Id;
            vm.Editor = editorOverride;
            return vm;
        }
        catch (HttpRequestException)
        {
            return new MapPoiManagementViewModel
            {
                LoadErrorMessage = "Không thể kết nối VKFoodAPI. Trang Map Analytics chỉ hoạt động khi API đang chạy.",
                SelectedPoiId = editorOverride?.Id ?? poiId ?? Guid.Empty,
                Editor = editorOverride ?? new PoiEditorViewModel
                {
                    Id = poiId ?? Guid.Empty,
                    IsEditMode = true
                }
            };
        }
    }

    private bool ApplyValidationResult(PoiValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError($"Editor.{error.FieldName}", error.Message);
        }

        return validation.IsValid;
    }
}

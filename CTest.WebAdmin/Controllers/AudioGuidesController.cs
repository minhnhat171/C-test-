using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class AudioGuidesController : Controller
{
    private readonly AudioGuideAdminService _audioGuideService;
    private readonly AudioGuideValidationService _validationService;

    public AudioGuidesController(
        AudioGuideAdminService audioGuideService,
        AudioGuideValidationService validationService)
    {
        _audioGuideService = audioGuideService;
        _validationService = validationService;
    }

    [HttpGet]
    public IActionResult Index(
        Guid? audioId,
        Guid? poiId,
        string? languageCode,
        bool createNew = false)
    {
        return poiId.HasValue
            ? RedirectToAction("Edit", "Pois", new { id = poiId.Value })
            : RedirectToAction("Index", "Pois");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        AudioGuideManagementPageViewModel model,
        CancellationToken cancellationToken = default)
    {
        var vm = await _audioGuideService.LoadManagementPageAsync(
            model.Editor.Id,
            model.ScopePoiId,
            model.Editor.LanguageCode,
            model.Editor.Id == Guid.Empty,
            cancellationToken);

        vm.Editor = model.Editor;
        vm.Editor.IsEditMode = model.Editor.Id != Guid.Empty;

        var validation = _validationService.Validate(vm.Editor, vm.Pois);
        if (!ApplyValidationResult(validation))
        {
            return View("Manage", vm);
        }

        try
        {
            var result = await _audioGuideService.SaveAsync(vm.Editor, vm.Pois, cancellationToken);
            TempData["AudioMessage"] = result.Message;

            return result.AudioGuideId.HasValue
                ? RedirectToAction(nameof(Index), new { audioId = result.AudioGuideId.Value, poiId = model.ScopePoiId, languageCode = model.Editor.LanguageCode })
                : RedirectToAction(nameof(Index), new { createNew = true, poiId = model.ScopePoiId, languageCode = model.Editor.LanguageCode });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể kết nối VKFoodAPI. Hãy mở API trước rồi lưu lại.");
            return View("Manage", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid id,
        Guid? scopePoiId,
        string? languageCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _audioGuideService.DeleteAsync(id, cancellationToken);
            TempData["AudioMessage"] = result.Message;
        }
        catch (HttpRequestException)
        {
            TempData["AudioMessage"] = "Không thể kết nối VKFoodAPI nên chưa xóa được audio.";
        }

        return RedirectToAction(nameof(Index), new { createNew = true, poiId = scopePoiId, languageCode });
    }

    private bool ApplyValidationResult(AudioGuideValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }

        return validation.IsValid;
    }
}


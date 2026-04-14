using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

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
    public async Task<IActionResult> Index(
        Guid? audioId,
        Guid? poiId,
        bool createNew = false,
        CancellationToken cancellationToken = default)
    {
        var vm = await _audioGuideService.LoadManagementPageAsync(audioId, poiId, createNew, cancellationToken);
        return View("Manage", vm);
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
                ? RedirectToAction(nameof(Index), new { audioId = result.AudioGuideId.Value, poiId = model.ScopePoiId })
                : RedirectToAction(nameof(Index), new { createNew = true, poiId = model.ScopePoiId });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Khong the ket noi VKFoodAPI. Hay mo API truoc roi luu lai.");
            return View("Manage", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid? scopePoiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _audioGuideService.DeleteAsync(id, cancellationToken);
            TempData["AudioMessage"] = result.Message;
        }
        catch (HttpRequestException)
        {
            TempData["AudioMessage"] = "Khong the ket noi VKFoodAPI nen chua xoa duoc audio.";
        }

        return RedirectToAction(nameof(Index), new { createNew = true, poiId = scopePoiId });
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


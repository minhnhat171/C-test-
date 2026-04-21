using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.OwnerArea)]
public class OwnerController : Controller
{
    private readonly PoiAdminService _poiService;
    private readonly PoiValidationService _validationService;
    private readonly ListeningHistoryService _listeningHistoryService;
    private readonly IWebAdminCurrentUser _currentUser;

    public OwnerController(
        PoiAdminService poiService,
        PoiValidationService validationService,
        ListeningHistoryService listeningHistoryService,
        IWebAdminCurrentUser currentUser)
    {
        _poiService = poiService;
        _validationService = validationService;
        _listeningHistoryService = listeningHistoryService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? sortBy = null,
        string? period = null,
        string? keyword = null,
        bool partial = false,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildPortalAsync(sortBy, period, keyword, cancellationToken);
        if (partial || IsAjaxRequest())
        {
            return PartialView("~/Views/UsageLogs/_UsageHistoryContent.cshtml", model.ListeningHistory);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        OwnerPortalViewModel model,
        CancellationToken cancellationToken = default)
    {
        var editor = model.Registration;
        editor.IsEditMode = false;
        editor.IsActive = true;

        try
        {
            var existingPois = await _poiService.LoadValidationSnapshotAsync(cancellationToken);
            var validation = _validationService.ValidateCreate(editor, existingPois);
            if (!ApplyValidationResult(validation, nameof(OwnerPortalViewModel.Registration)))
            {
                var invalidModel = await BuildPortalAsync(null, null, null, cancellationToken);
                invalidModel.Registration = editor;
                return View("Index", invalidModel);
            }

            var result = await _poiService.CreateAsync(editor, cancellationToken);
            if (!result.Succeeded)
            {
                ModelState.AddModelError($"{nameof(OwnerPortalViewModel.Registration)}.{nameof(PoiEditorViewModel.UploadedImage)}", result.Message);
                var invalidModel = await BuildPortalAsync(null, null, null, cancellationToken);
                invalidModel.Registration = editor;
                return View("Index", invalidModel);
            }

            TempData["OwnerMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Khong the ket noi VKFoodAPI. Hay mo API truoc roi dang ky lai.");
            var invalidModel = await BuildPortalAsync(null, null, null, cancellationToken);
            invalidModel.Registration = editor;
            return View("Index", invalidModel);
        }
    }

    private async Task<OwnerPortalViewModel> BuildPortalAsync(
        string? sortBy,
        string? period,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var pois = new PoiManagementViewModel();
        var history = new ListeningHistoryPageViewModel();

        try
        {
            pois = await _poiService.LoadManagementPageAsync(null, "all", cancellationToken);
            history = await _listeningHistoryService.LoadPageForPoisAsync(
                pois.Items.Select(item => item.Id),
                sortBy,
                period,
                keyword,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            pois.LoadErrorMessage = "Khong the ket noi VKFoodAPI. Hay mo API de dang ky POI va xem lich su nghe.";
            history.LoadErrorMessage = pois.LoadErrorMessage;
        }

        return new OwnerPortalViewModel
        {
            OwnerDisplayName = _currentUser.DisplayName,
            Pois = pois,
            ListeningHistory = history,
            Registration = new PoiEditorViewModel
            {
                OwnerUserCode = _currentUser.OwnerCode,
                OwnerDisplayName = _currentUser.DisplayName,
                OwnerEmail = _currentUser.OwnerEmail,
                RadiusInMeters = 50,
                CooldownMinutes = 5,
                IsActive = true
            }
        };
    }

    private bool ApplyValidationResult(PoiValidationResult validation, string prefix)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError($"{prefix}.{error.FieldName}", error.Message);
        }

        return validation.IsValid;
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}

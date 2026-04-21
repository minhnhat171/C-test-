using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class TranslationsController : Controller
{
    private readonly PoiApiClient _poiApiClient;
    private static readonly (string Code, string Label)[] SupportedLanguages =
    [
        ("vi", "Tieng Viet"),
        ("en", "Tieng Anh"),
        ("zh", "Tieng Trung"),
        ("ko", "Tieng Han"),
        ("fr", "Tieng Phap")
    ];

    public TranslationsController(PoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
    }

    public async Task<IActionResult> Index(Guid? poiId, CancellationToken cancellationToken = default)
    {
        var vm = new TranslationManagementViewModel();

        try
        {
            vm.Pois = (await _poiApiClient.GetPoisAsync(cancellationToken))
                .OrderBy(x => x.Name)
                .Select(x => x.ToLookupItem())
                .ToList();

            if (!vm.Pois.Any())
            {
                return View("Manage", vm);
            }

            var selectedPoi = vm.Pois.FirstOrDefault(x => x.Id == poiId) ?? vm.Pois.First();
            var poi = await _poiApiClient.GetPoiAsync(selectedPoi.Id, cancellationToken);
            if (poi is null)
            {
                vm.LoadErrorMessage = "Khong tai duoc du lieu POI tu API.";
                return View("Manage", vm);
            }

            vm.SelectedPoiId = poi.Id;
            vm.SelectedPoiName = poi.Name;
            vm.Languages = SupportedLanguages
                .Select(language => BuildLanguageEditor(poi, language.Code, language.Label))
                .ToList();
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Khong the ket noi VKFoodAPI. Trang ban dich chi dong bo khi API dang chay.";
        }

        return View("Manage", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(TranslationLanguageEditorViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.PoiId == Guid.Empty)
        {
            TempData["TranslationMessage"] = "Khong xac dinh duoc POI de luu ban dich.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var poi = await _poiApiClient.GetPoiAsync(model.PoiId, cancellationToken);
            if (poi is null)
            {
                TempData["TranslationMessage"] = "POI khong con ton tai tren API.";
                return RedirectToAction(nameof(Index));
            }

            poi.NarrationTranslations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var narration = model.NarrationScript?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(narration))
            {
                poi.NarrationTranslations.Remove(model.LanguageCode);
            }
            else
            {
                poi.NarrationTranslations[model.LanguageCode] = narration;
            }

            if (string.Equals(model.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            {
                poi.NarrationText = narration;
            }

            await _poiApiClient.UpdatePoiAsync(poi.Id, poi, cancellationToken);

            TempData["TranslationMessage"] = $"Da cap nhat script {model.LanguageLabel} cho app MAUI.";
            return RedirectToAction(nameof(Index), new { poiId = poi.Id });
        }
        catch (HttpRequestException)
        {
            TempData["TranslationMessage"] = "Khong the ket noi VKFoodAPI nen chua luu duoc ban dich.";
            return RedirectToAction(nameof(Index), new { poiId = model.PoiId });
        }
    }

    private static TranslationLanguageEditorViewModel BuildLanguageEditor(PoiDto poi, string languageCode, string languageLabel)
    {
        var translations = poi.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        translations.TryGetValue(languageCode, out var translatedNarration);

        var narrationScript = string.Equals(languageCode, "vi", StringComparison.OrdinalIgnoreCase)
            ? (!string.IsNullOrWhiteSpace(poi.NarrationText) ? poi.NarrationText : translatedNarration ?? string.Empty)
            : translatedNarration ?? string.Empty;

        return new TranslationLanguageEditorViewModel
        {
            PoiId = poi.Id,
            LanguageCode = languageCode,
            LanguageLabel = languageLabel,
            Title = poi.Name,
            Description = poi.Description,
            NarrationScript = narrationScript,
            IsTranslated = !string.IsNullOrWhiteSpace(narrationScript)
        };
    }
}

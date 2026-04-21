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
        ("vi", "Tiếng Việt"),
        ("en", "Tiếng Anh"),
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
                vm.LoadErrorMessage = "Không tải được dữ liệu POI từ API.";
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
            vm.LoadErrorMessage = "Không thể kết nối VKFoodAPI. Trang bản dịch chỉ đồng bộ khi API đang chạy.";
        }

        return View("Manage", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(TranslationLanguageEditorViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.PoiId == Guid.Empty)
        {
            TempData["TranslationMessage"] = "Không xác định được POI để lưu bản dịch.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var poi = await _poiApiClient.GetPoiAsync(model.PoiId, cancellationToken);
            if (poi is null)
            {
                TempData["TranslationMessage"] = "POI không còn tồn tại trên API.";
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

            TempData["TranslationMessage"] = $"Đã cập nhật script {model.LanguageLabel} cho app MAUI.";
            return RedirectToAction(nameof(Index), new { poiId = poi.Id });
        }
        catch (HttpRequestException)
        {
            TempData["TranslationMessage"] = "Không thể kết nối VKFoodAPI nên chưa lưu được bản dịch.";
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

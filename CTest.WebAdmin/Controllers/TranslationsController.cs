using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class TranslationsController : Controller
{
    private readonly AppDataService _data;
    private readonly WebDisplayClock _clock;
    private static readonly (string Code, string Label)[] SupportedLanguages =
    [
        ("vi", "Tiếng Việt"),
        ("en", "Tiếng Anh"),
        ("zh", "Tiếng Trung"),
        ("ko", "Tiếng Hàn")
    ];

    public TranslationsController(AppDataService data, WebDisplayClock clock)
    {
        _data = data;
        _clock = clock;
    }

    public IActionResult Index(int? poiId)
    {
        var pois = _data.Pois.OrderBy(x => x.Name).ToList();
        if (!pois.Any())
        {
            return View("Manage", new TranslationManagementViewModel());
        }

        var selectedPoi = pois.FirstOrDefault(x => x.Id == poiId) ?? pois.First();

        var vm = new TranslationManagementViewModel
        {
            Pois = pois,
            SelectedPoiId = selectedPoi.Id,
            SelectedPoiName = selectedPoi.Name,
            Languages = SupportedLanguages.Select(language => BuildLanguageEditor(selectedPoi, language.Code, language.Label)).ToList()
        };

        return View("Manage", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(TranslationLanguageEditorViewModel model)
    {
        var poi = _data.Pois.FirstOrDefault(x => x.Id == model.PoiId);
        if (poi is null)
        {
            TempData["TranslationMessage"] = "Không tìm thấy POI để lưu bản dịch.";
            return RedirectToAction(nameof(Index));
        }

        var entry = _data.Translations.FirstOrDefault(x => x.Id == model.TranslationId);
        if (entry is null)
        {
            entry = _data.Translations.FirstOrDefault(x => x.PoiId == model.PoiId && NormalizeLanguageCode(x.Language) == model.LanguageCode);
        }

        if (entry is null)
        {
            entry = new TranslationEntry
            {
                Id = _data.Translations.Any() ? _data.Translations.Max(x => x.Id) + 1 : 1,
                PoiId = poi.Id,
                PoiName = poi.Name,
                Language = model.LanguageLabel
            };

            _data.Translations.Add(entry);
        }

        entry.PoiId = poi.Id;
        entry.PoiName = poi.Name;
        entry.Language = model.LanguageLabel;
        entry.Title = model.Title;
        entry.Description = model.Description;
        entry.NarrationScript = model.NarrationScript;
        entry.Body = model.NarrationScript;
        entry.IsApproved = model.IsTranslated;
        entry.UpdatedAt = _clock.Now.DateTime;

        TempData["TranslationMessage"] = $"Đã cập nhật nội dung {model.LanguageLabel} cho {poi.Name}.";
        return RedirectToAction(nameof(Index), new { poiId = poi.Id });
    }

    private TranslationLanguageEditorViewModel BuildLanguageEditor(Poi poi, string languageCode, string languageLabel)
    {
        var entry = _data.Translations.FirstOrDefault(x => x.PoiId == poi.Id && NormalizeLanguageCode(x.Language) == languageCode);

        return new TranslationLanguageEditorViewModel
        {
            TranslationId = entry?.Id ?? 0,
            PoiId = poi.Id,
            LanguageCode = languageCode,
            LanguageLabel = languageLabel,
            Title = entry?.Title ?? poi.Name,
            Description = entry?.Description ?? poi.Description,
            NarrationScript = entry?.NarrationScript ?? entry?.Body ?? poi.NarrationScript,
            IsTranslated = entry?.IsApproved ?? false
        };
    }

    private static string NormalizeLanguageCode(string language)
    {
        var value = language.ToLowerInvariant();

        if (value.Contains("vi"))
        {
            return "vi";
        }

        if (value.Contains("en"))
        {
            return "en";
        }

        if (value.Contains("zh") || value.Contains("trung") || value.Contains("chinese"))
        {
            return "zh";
        }

        if (value.Contains("ko") || value.Contains("hàn") || value.Contains("korean"))
        {
            return "ko";
        }

        return value;
    }
}

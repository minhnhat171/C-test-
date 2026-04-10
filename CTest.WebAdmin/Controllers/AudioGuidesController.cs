using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class AudioGuidesController : Controller
{
    private readonly AppDataService _data;

    public AudioGuidesController(AppDataService data)
    {
        _data = data;
    }

    public IActionResult Index(int? audioId, bool createNew = false)
    {
        var editor = createNew
            ? new AudioGuideEditorViewModel()
            : BuildEditorViewModel(_data.AudioGuides.FirstOrDefault(x => x.Id == audioId) ?? _data.AudioGuides.OrderBy(x => x.Id).FirstOrDefault());

        var vm = new AudioManagementViewModel
        {
            Items = _data.AudioGuides.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id).ToList(),
            Pois = _data.Pois.OrderBy(x => x.Name).ToList(),
            Editor = editor
        };

        return View("Manage", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(AudioManagementViewModel model)
    {
        var editor = model.Editor;
        var poi = _data.Pois.FirstOrDefault(x => x.Id == editor.PoiId);
        if (poi is null)
        {
            TempData["AudioMessage"] = "Không tìm thấy POI để lưu audio.";
            return RedirectToAction(nameof(Index));
        }

        var guide = _data.AudioGuides.FirstOrDefault(x => x.Id == editor.Id);
        if (guide is null)
        {
            guide = new AudioGuide
            {
                Id = _data.AudioGuides.Any() ? _data.AudioGuides.Max(x => x.Id) + 1 : 1
            };

            _data.AudioGuides.Add(guide);
        }

        guide.PoiId = editor.PoiId;
        guide.PoiName = poi.Name;
        guide.Language = editor.Language;
        guide.VoiceType = editor.VoiceType;
        guide.SourceType = editor.SourceType;
        guide.ContentOrFile = editor.SourceType == "File" ? editor.FilePath : editor.Script;
        guide.EstimatedSeconds = editor.EstimatedSeconds > 0 ? editor.EstimatedSeconds : Math.Max(15, (int)Math.Ceiling((editor.Script?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0) / 2.5));
        guide.IsPublished = editor.IsPublished;
        guide.UpdatedAt = DateTime.Now;

        TempData["AudioMessage"] = guide.Id == editor.Id && editor.Id != 0
            ? "Đã cập nhật audio."
            : "Đã tạo audio mới.";

        return RedirectToAction(nameof(Index), new { audioId = guide.Id });
    }

    private static AudioGuideEditorViewModel BuildEditorViewModel(AudioGuide? guide)
    {
        if (guide is null)
        {
            return new AudioGuideEditorViewModel();
        }

        var isFile = string.Equals(guide.SourceType, "File", StringComparison.OrdinalIgnoreCase);

        return new AudioGuideEditorViewModel
        {
            Id = guide.Id,
            PoiId = guide.PoiId,
            Language = guide.Language,
            VoiceType = guide.VoiceType,
            SourceType = guide.SourceType,
            Script = isFile ? string.Empty : guide.ContentOrFile,
            FilePath = isFile ? guide.ContentOrFile : string.Empty,
            EstimatedSeconds = guide.EstimatedSeconds,
            IsPublished = guide.IsPublished
        };
    }
}

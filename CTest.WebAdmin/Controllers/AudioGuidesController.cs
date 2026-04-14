using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class AudioGuidesController : Controller
{
    private readonly AudioGuideApiClient _audioGuideApiClient;
    private readonly PoiApiClient _poiApiClient;

    public AudioGuidesController(
        AudioGuideApiClient audioGuideApiClient,
        PoiApiClient poiApiClient)
    {
        _audioGuideApiClient = audioGuideApiClient;
        _poiApiClient = poiApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? audioId,
        bool createNew = false,
        CancellationToken cancellationToken = default)
    {
        var vm = await BuildPageModelAsync(audioId, createNew, cancellationToken);
        return View("ManageApi", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        AudioGuideManagementPageViewModel model,
        CancellationToken cancellationToken = default)
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

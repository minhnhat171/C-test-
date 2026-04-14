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
        var vm = await BuildPageModelAsync(model.Editor.Id, model.Editor.Id == Guid.Empty, cancellationToken);
        vm.Editor = model.Editor;
        vm.Editor.IsEditMode = model.Editor.Id != Guid.Empty;

        ValidateEditor(vm.Editor, vm.Pois);
        if (!ModelState.IsValid)
        {
            return View("ManageApi", vm);
        }

        try
        {
            var poiLookup = vm.Pois.ToDictionary(item => item.Id);
            var dto = vm.Editor.ToDto(poiLookup);

            if (vm.Editor.Id == Guid.Empty)
            {
                var created = await _audioGuideApiClient.CreateAudioGuideAsync(dto, cancellationToken);
                TempData["AudioMessage"] = "Đã tạo audio mới và đồng bộ sang VKFoodAPI.";
                return RedirectToAction(nameof(Index), new { audioId = created.Id });
            }

            var updated = await _audioGuideApiClient.UpdateAudioGuideAsync(vm.Editor.Id, dto, cancellationToken);
            TempData["AudioMessage"] = updated
                ? "Đã cập nhật audio và lưu vào VKFoodAPI."
                : "Không tìm thấy audio cần cập nhật trên VKFoodAPI.";

            return RedirectToAction(nameof(Index), new { audioId = vm.Editor.Id });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối VKFoodAPI. Hãy mở API trước rồi lưu lại.");
            return View("ManageApi", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            TempData["AudioMessage"] = "Không xác định được audio cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var deleted = await _audioGuideApiClient.DeleteAudioGuideAsync(id, cancellationToken);
            TempData["AudioMessage"] = deleted
                ? "Đã xóa audio khỏi VKFoodAPI."
                : "Không tìm thấy audio để xóa.";
        }
        catch (HttpRequestException)
        {
            TempData["AudioMessage"] = "Không thể kết nối VKFoodAPI nên chưa xóa được audio.";
        }

        return RedirectToAction(nameof(Index), new { createNew = true });
    }

    private async Task<AudioGuideManagementPageViewModel> BuildPageModelAsync(
        Guid? audioId,
        bool createNew,
        CancellationToken cancellationToken)
    {
        var vm = new AudioGuideManagementPageViewModel();

        try
        {
            var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
            var audioGuidesTask = _audioGuideApiClient.GetAudioGuidesAsync(cancellationToken);

            await Task.WhenAll(poisTask, audioGuidesTask);

            vm.Pois = poisTask.Result
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Name)
                .Select(item => item.ToLookupItem())
                .ToList();

            vm.Items = audioGuidesTask.Result
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.PoiName)
                .Select(item => item.ToListItem(item.Id == audioId))
                .ToList();

            if (createNew || !vm.Items.Any())
            {
                vm.Editor = new AudioGuideEditorFormViewModel();
                return vm;
            }

            var selectedGuide = audioId.HasValue
                ? audioGuidesTask.Result.FirstOrDefault(item => item.Id == audioId.Value)
                : audioGuidesTask.Result.FirstOrDefault();

            vm.Editor = selectedGuide?.ToEditorViewModel() ?? new AudioGuideEditorFormViewModel();
            return vm;
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Không thể kết nối VKFoodAPI. Phần Audio / TTS chỉ đồng bộ khi API đang chạy.";
            vm.Editor = new AudioGuideEditorFormViewModel();
            return vm;
        }
    }

    private void ValidateEditor(
        AudioGuideEditorFormViewModel editor,
        IReadOnlyList<PoiLookupItemViewModel> pois)
    {
        if (editor.PoiId == Guid.Empty)
        {
            ModelState.AddModelError("Editor.PoiId", "Hãy chọn POI cần gắn audio.");
        }
        else if (!pois.Any(item => item.Id == editor.PoiId))
        {
            ModelState.AddModelError("Editor.PoiId", "POI đã chọn không còn tồn tại trên API.");
        }

        if (string.Equals(editor.SourceType, "file", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(editor.FilePath))
            {
                ModelState.AddModelError("Editor.FilePath", "Hãy nhập đường dẫn file audio.");
            }
        }
        else if (string.IsNullOrWhiteSpace(editor.Script))
        {
            ModelState.AddModelError("Editor.Script", "Hãy nhập nội dung script cho TTS.");
        }

        if (editor.EstimatedSeconds < 0)
        {
            ModelState.AddModelError("Editor.EstimatedSeconds", "Thời lượng dự kiến không được nhỏ hơn 0.");
        }
    }
}

using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class AudioGuideAdminService
{
    private static readonly (string Code, string Label)[] SupportedLanguages =
    [
        ("vi", "Tieng Viet"),
        ("en", "Tieng Anh"),
        ("zh", "Tieng Trung"),
        ("ko", "Tieng Han"),
        ("fr", "Tieng Phap")
    ];

    private readonly AudioGuideApiClient _audioGuideApiClient;
    private readonly PoiApiClient _poiApiClient;

    public AudioGuideAdminService(
        AudioGuideApiClient audioGuideApiClient,
        PoiApiClient poiApiClient)
    {
        _audioGuideApiClient = audioGuideApiClient;
        _poiApiClient = poiApiClient;
    }

    public async Task<AudioGuideManagementPageViewModel> LoadManagementPageAsync(
        Guid? audioId,
        Guid? poiId,
        string? languageCode,
        bool createNew,
        CancellationToken cancellationToken = default)
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

            if (poiId.HasValue)
            {
                var scopedPoi = vm.Pois.FirstOrDefault(item => item.Id == poiId.Value);
                if (scopedPoi is not null)
                {
                    vm.ScopePoiId = scopedPoi.Id;
                    vm.ScopePoiName = scopedPoi.Name;
                    vm.ScopePoiCode = scopedPoi.Code;
                }
            }

            var scopedAudioGuides = audioGuidesTask.Result
                .Where(item => !vm.ScopePoiId.HasValue || item.PoiId == vm.ScopePoiId.Value)
                .ToList();

            var normalizedLanguageCode = AudioGuideAdminMappings.NormalizeLanguageCode(languageCode);
            var selectedGuide = ResolveSelectedGuide(
                scopedAudioGuides,
                audioId,
                normalizedLanguageCode,
                createNew);

            vm.Items = scopedAudioGuides
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.PoiName)
                .Select(item => item.ToListItem(item.Id == selectedGuide?.Id))
                .ToList();

            vm.LanguageSlots = vm.ScopePoiId.HasValue
                ? BuildLanguageSlots(scopedAudioGuides, normalizedLanguageCode)
                : new List<AudioGuideLanguageSlotViewModel>();

            if (createNew || selectedGuide is null)
            {
                vm.Editor = new AudioGuideEditorFormViewModel
                {
                    PoiId = vm.ScopePoiId ?? Guid.Empty,
                    LanguageCode = normalizedLanguageCode
                };
                return vm;
            }

            vm.Editor = selectedGuide.ToEditorViewModel();
            return vm;
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Khong the ket noi VKFoodAPI. Phan Audio / TTS chi dong bo khi API dang chay.";
            vm.Editor = new AudioGuideEditorFormViewModel();
            return vm;
        }
    }

    public async Task<AudioGuideOperationResult> SaveAsync(
        AudioGuideEditorFormViewModel editor,
        IReadOnlyList<PoiLookupItemViewModel> pois,
        CancellationToken cancellationToken = default)
    {
        var poiLookup = pois.ToDictionary(item => item.Id);
        var dto = editor.ToDto(poiLookup);

        if (editor.Id == Guid.Empty)
        {
            var created = await _audioGuideApiClient.CreateAudioGuideAsync(dto, cancellationToken);
            return AudioGuideOperationResult.Success(
                "Da tao audio moi va dong bo sang VKFoodAPI.",
                created.Id);
        }

        var updated = await _audioGuideApiClient.UpdateAudioGuideAsync(editor.Id, dto, cancellationToken);

        return updated
            ? AudioGuideOperationResult.Success(
                "Da cap nhat audio va luu vao VKFoodAPI.",
                editor.Id)
            : AudioGuideOperationResult.Missing("Khong tim thay audio can cap nhat tren VKFoodAPI.");
    }

    public async Task<AudioGuideOperationResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return AudioGuideOperationResult.Failure("Khong xac dinh duoc audio can xoa.");
        }

        var deleted = await _audioGuideApiClient.DeleteAudioGuideAsync(id, cancellationToken);

        return deleted
            ? AudioGuideOperationResult.Success("Da xoa audio khoi VKFoodAPI.", id)
            : AudioGuideOperationResult.Missing("Khong tim thay audio de xoa.");
    }

    private static AudioGuideDto? ResolveSelectedGuide(
        IReadOnlyList<AudioGuideDto> scopedAudioGuides,
        Guid? audioId,
        string normalizedLanguageCode,
        bool createNew)
    {
        if (createNew || scopedAudioGuides.Count == 0)
        {
            return null;
        }

        if (audioId.HasValue)
        {
            var guideById = scopedAudioGuides.FirstOrDefault(item => item.Id == audioId.Value);
            if (guideById is not null)
            {
                return guideById;
            }
        }

        var guideByLanguage = scopedAudioGuides
            .Where(item => string.Equals(
                AudioGuideAdminMappings.NormalizeLanguageCode(item.LanguageCode),
                normalizedLanguageCode,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();

        return guideByLanguage ?? scopedAudioGuides.FirstOrDefault();
    }

    private static List<AudioGuideLanguageSlotViewModel> BuildLanguageSlots(
        IReadOnlyList<AudioGuideDto> scopedAudioGuides,
        string selectedLanguageCode)
    {
        return SupportedLanguages
            .Select(language =>
            {
                var latestGuide = scopedAudioGuides
                    .Where(item => string.Equals(
                        AudioGuideAdminMappings.NormalizeLanguageCode(item.LanguageCode),
                        language.Code,
                        StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.UpdatedAtUtc)
                    .FirstOrDefault();

                var hasAudio = latestGuide is not null;
                var isPublished = latestGuide?.IsPublished ?? false;

                return new AudioGuideLanguageSlotViewModel
                {
                    LanguageCode = language.Code,
                    LanguageLabel = language.Label,
                    StatusLabel = !hasAudio
                        ? "Chua co noi dung"
                        : isPublished
                            ? "Dang publish"
                            : "Ban nhap",
                    StatusCssClass = !hasAudio
                        ? "empty"
                        : isPublished
                            ? "published"
                            : "draft",
                    SourceLabel = hasAudio
                        ? AudioGuideAdminMappings.GetSourceLabel(latestGuide!.SourceType)
                        : "Chua co audio",
                    UpdatedLabel = hasAudio
                        ? latestGuide!.UpdatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                        : "Chua cap nhat",
                    AudioId = latestGuide?.Id,
                    HasAudio = hasAudio,
                    IsPublished = isPublished,
                    IsSelected = string.Equals(language.Code, selectedLanguageCode, StringComparison.OrdinalIgnoreCase)
                };
            })
            .ToList();
    }
}

public sealed record AudioGuideOperationResult(
    bool Succeeded,
    bool NotFound,
    string Message,
    Guid? AudioGuideId)
{
    public static AudioGuideOperationResult Success(string message, Guid audioGuideId)
        => new(true, false, message, audioGuideId);

    public static AudioGuideOperationResult Missing(string message)
        => new(false, true, message, null);

    public static AudioGuideOperationResult Failure(string message)
        => new(false, false, message, null);
}

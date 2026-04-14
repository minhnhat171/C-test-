using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Services;

public class AudioGuideAdminService
{
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

            vm.Items = scopedAudioGuides
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.PoiName)
                .Select(item => item.ToListItem(item.Id == audioId))
                .ToList();

            if (createNew || !vm.Items.Any())
            {
                vm.Editor = new AudioGuideEditorFormViewModel
                {
                    PoiId = vm.ScopePoiId ?? Guid.Empty
                };
                return vm;
            }

            var selectedGuide = audioId.HasValue
                ? scopedAudioGuides.FirstOrDefault(item => item.Id == audioId.Value)
                : scopedAudioGuides.FirstOrDefault();

            vm.Editor = selectedGuide?.ToEditorViewModel() ?? new AudioGuideEditorFormViewModel
            {
                PoiId = vm.ScopePoiId ?? Guid.Empty
            };
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

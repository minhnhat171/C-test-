using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Services;

public class AudioGuideValidationService
{
    public AudioGuideValidationResult Validate(
        AudioGuideEditorFormViewModel model,
        IReadOnlyList<PoiLookupItemViewModel> pois)
    {
        var errors = new List<AudioGuideValidationError>();

        if (model.PoiId == Guid.Empty)
        {
            errors.Add(new AudioGuideValidationError("Editor.PoiId", "Hay chon POI can gan audio."));
        }
        else if (!pois.Any(item => item.Id == model.PoiId))
        {
            errors.Add(new AudioGuideValidationError("Editor.PoiId", "POI đã chọn không còn tồn tại trên API."));
        }

        if (string.Equals(model.SourceType, "file", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(model.FilePath))
            {
                errors.Add(new AudioGuideValidationError("Editor.FilePath", "Hay nhap duong dan file audio."));
            }
        }
        else if (string.IsNullOrWhiteSpace(model.Script))
        {
            errors.Add(new AudioGuideValidationError("Editor.Script", "Hãy nhập nội dung script cho TTS."));
        }

        if (model.EstimatedSeconds < 0)
        {
            errors.Add(new AudioGuideValidationError(
                "Editor.EstimatedSeconds",
                "Thời lượng dự kiến không được nhỏ hơn 0."));
        }

        return new AudioGuideValidationResult(errors.Count == 0, errors);
    }
}

public sealed record AudioGuideValidationResult(
    bool IsValid,
    IReadOnlyList<AudioGuideValidationError> Errors);

public sealed record AudioGuideValidationError(
    string FieldName,
    string Message);

using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Services;

public class PoiValidationService
{
    public PoiValidationResult ValidateCreate(
        PoiEditorViewModel model,
        IReadOnlyList<PoiListItemViewModel> existingPois)
    {
        return Validate(model, existingPois, currentPoiId: null);
    }

    public PoiValidationResult ValidateUpdate(
        PoiEditorViewModel model,
        IReadOnlyList<PoiListItemViewModel> existingPois)
    {
        var errors = new List<PoiValidationError>();
        if (model.Id == Guid.Empty)
        {
            errors.Add(new PoiValidationError(nameof(model.Id), "Không xác định được POI cần cập nhật."));
        }

        errors.AddRange(Validate(model, existingPois, model.Id).Errors);
        return new PoiValidationResult(errors.Count == 0, errors);
    }

    private static PoiValidationResult Validate(
        PoiEditorViewModel model,
        IReadOnlyList<PoiListItemViewModel> existingPois,
        Guid? currentPoiId)
    {
        var errors = new List<PoiValidationError>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            errors.Add(new PoiValidationError(nameof(model.Name), "Tên điểm không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(model.Address))
        {
            errors.Add(new PoiValidationError(nameof(model.Address), "Địa chỉ không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            errors.Add(new PoiValidationError(nameof(model.Description), "Mô tả không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(model.NarrationScript))
        {
            errors.Add(new PoiValidationError(nameof(model.NarrationScript), "Script thuyết minh không được để trống."));
        }

        if (model.Latitude is < -90 or > 90)
        {
            errors.Add(new PoiValidationError(nameof(model.Latitude), "Latitude phải nằm trong khoảng -90 đến 90."));
        }

        if (model.Longitude is < -180 or > 180)
        {
            errors.Add(new PoiValidationError(nameof(model.Longitude), "Longitude phải nằm trong khoảng -180 đến 180."));
        }

        if (model.RadiusInMeters <= 0)
        {
            errors.Add(new PoiValidationError(nameof(model.RadiusInMeters), "Bán kính kích hoạt phải lớn hơn 0."));
        }

        if (model.Priority <= 0)
        {
            errors.Add(new PoiValidationError(nameof(model.Priority), "Mức ưu tiên phải lớn hơn 0."));
        }

        if (model.CooldownMinutes <= 0)
        {
            errors.Add(new PoiValidationError(nameof(model.CooldownMinutes), "Cooldown phải lớn hơn 0 phút."));
        }

        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var duplicatedCode = existingPois.Any(x =>
                (!currentPoiId.HasValue || x.Id != currentPoiId.Value) &&
                string.Equals(x.Code, model.Code.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicatedCode)
            {
                errors.Add(new PoiValidationError(nameof(model.Code), "Mã POI đã tồn tại. Vui lòng dùng mã khác."));
            }
        }

        if (!string.IsNullOrWhiteSpace(model.MapLink) &&
            !Uri.TryCreate(model.MapLink, UriKind.Absolute, out _))
        {
            errors.Add(new PoiValidationError(nameof(model.MapLink), "Map link phải là URL hợp lệ."));
        }

        return new PoiValidationResult(errors.Count == 0, errors);
    }
}

public sealed record PoiValidationResult(
    bool IsValid,
    IReadOnlyList<PoiValidationError> Errors);

public sealed record PoiValidationError(
    string FieldName,
    string Message);

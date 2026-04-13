using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface IAccountProfileValidationService
{
    AccountProfileValidationResult Validate(AccountProfileUpdateRequest request);
}

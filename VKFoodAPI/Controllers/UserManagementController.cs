using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/admin/users")]
public class UserManagementController : ControllerBase
{
    private readonly UserManagementRepository _repository;

    public UserManagementController(UserManagementRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<AdminUserSummaryDto>> GetAllUsers()
    {
        return Ok(_repository.GetAllUsers());
    }

    [HttpGet("by-status")]
    public ActionResult<IEnumerable<AdminUserSummaryDto>> GetUsersByStatus([FromQuery] string? status = null)
    {
        return Ok(_repository.GetUsersByStatus(status));
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<AdminUserSummaryDto>> SearchUsers([FromQuery] string? keyword = null)
    {
        return Ok(_repository.SearchUsers(keyword));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<AdminUserDetailDto> GetUserDetails(Guid id)
    {
        var detail = _repository.GetUserDetails(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{id:guid}/location")]
    public ActionResult<AdminUserLocationDto> GetUserLocation(Guid id)
    {
        var location = _repository.GetUserLocation(id);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost("profile-sync")]
    public ActionResult<AdminUserDetailDto> UpsertProfile([FromBody] AdminUserProfileUpsertRequest request)
    {
        var detail = _repository.UpsertProfile(request);
        return Ok(detail);
    }
}

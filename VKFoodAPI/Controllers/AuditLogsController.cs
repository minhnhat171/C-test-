using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly AuditLogRepository _repository;

    public AuditLogsController(AuditLogRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<AuditLogDto>> GetLogs(
        [FromQuery] string? entityName = null,
        [FromQuery] string? username = null,
        [FromQuery] int? limit = null)
    {
        return Ok(_repository.GetLogs(entityName, username, limit));
    }

    [HttpPost]
    public ActionResult<AuditLogDto> Create([FromBody] AuditLogCreateRequest request)
    {
        var created = _repository.Create(request);
        return CreatedAtAction(nameof(GetLogs), new { id = created.Id }, created);
    }
}

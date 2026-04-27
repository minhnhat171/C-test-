using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/qr-codes")]
[Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
public sealed class QrCodesController : ControllerBase
{
    private readonly QrCodeRepository _repository;
    private readonly AuditLogRepository _auditLogRepository;

    public QrCodesController(
        QrCodeRepository repository,
        AuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<QrCodeItemDto>> GetAll()
    {
        return Ok(_repository.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<QrCodeItemDto> GetById(Guid id)
    {
        var item = _repository.GetById(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public ActionResult<QrCodeItemDto> Create([FromBody] QrCodeItemSaveRequest request)
    {
        try
        {
            var created = _repository.Create(request);
            WriteAudit("create", created);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] QrCodeItemSaveRequest request)
    {
        try
        {
            if (!_repository.Update(id, request))
            {
                return NotFound();
            }

            var updated = _repository.GetById(id);
            if (updated is not null)
            {
                WriteAudit("update", updated);
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var existing = _repository.GetById(id);
        if (!_repository.Delete(id))
        {
            return NotFound();
        }

        if (existing is not null)
        {
            WriteAudit("delete", existing);
        }

        return NoContent();
    }

    private void WriteAudit(string action, QrCodeItemDto item)
    {
        _auditLogRepository.Create(new AuditLogCreateRequest
        {
            Username = User.Identity?.Name ?? "WebAdmin",
            Action = action,
            EntityName = "QrCodeItem",
            EntityId = item.Id.ToString(),
            Description = $"{action} QR {item.Code} -> {item.TargetType}/{item.TargetId}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });
    }
}

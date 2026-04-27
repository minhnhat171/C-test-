using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly TourRepository _repo;
    private readonly AuditLogRepository _auditLogRepository;

    public ToursController(
        TourRepository repo,
        AuditLogRepository auditLogRepository)
    {
        _repo = repo;
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TourDto>> GetAll()
    {
        return Ok(_repo.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<TourDto> GetById(int id)
    {
        var tour = _repo.GetById(id);
        if (tour is null)
        {
            return NotFound();
        }

        return Ok(tour);
    }

    [HttpPost]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public ActionResult<TourDto> Create([FromBody] TourDto dto)
    {
        if (dto.PoiIds is null || dto.PoiIds.Count == 0)
        {
            ModelState.AddModelError(nameof(TourDto.PoiIds), "Tour phải có ít nhất một POI.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = _repo.Create(dto);
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

    [HttpPut("{id:int}")]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public IActionResult Update(int id, [FromBody] TourDto dto)
    {
        if (dto.PoiIds is null || dto.PoiIds.Count == 0)
        {
            ModelState.AddModelError(nameof(TourDto.PoiIds), "Tour phải có ít nhất một POI.");
            return ValidationProblem(ModelState);
        }

        try
        {
            if (!_repo.Update(id, dto))
            {
                return NotFound();
            }

            var updated = _repo.GetById(id);
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

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public IActionResult Delete(int id)
    {
        var existing = _repo.GetById(id);
        if (!_repo.Delete(id))
        {
            return NotFound();
        }

        if (existing is not null)
        {
            WriteAudit("delete", existing);
        }

        return NoContent();
    }

    private void WriteAudit(string action, TourDto tour)
    {
        _auditLogRepository.Create(new AuditLogCreateRequest
        {
            Username = User.Identity?.Name ?? "WebAdmin",
            Action = action,
            EntityName = "Tour",
            EntityId = tour.Id.ToString(),
            Description = $"{action} tour {tour.Code} - {tour.Name}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });
    }
}

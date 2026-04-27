using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoisController : ControllerBase
{
    private readonly PoiRepository _repo;
    private readonly QrCodeRepository _qrCodeRepository;
    private readonly AuditLogRepository _auditLogRepository;

    public PoisController(
        PoiRepository repo,
        QrCodeRepository qrCodeRepository,
        AuditLogRepository auditLogRepository)
    {
        _repo = repo;
        _qrCodeRepository = qrCodeRepository;
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PoiDto>> GetAll()
    {
        return Ok(_repo.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PoiDto> GetById(Guid id)
    {
        var poi = _repo.GetById(id);
        if (poi == null)
            return NotFound();

        return Ok(poi);
    }

    [HttpGet("by-qr")]
    public ActionResult<PoiDto> GetByQr([FromQuery] string? code)
    {
        if (!TryResolvePoiFromQr(code, out var poi))
        {
            return NotFound();
        }

        return Ok(poi);
    }

    [HttpPost]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public ActionResult<PoiDto> Create([FromBody] PoiDto dto)
    {
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

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public IActionResult Update(Guid id, [FromBody] PoiDto dto)
    {
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

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public IActionResult Delete(Guid id)
    {
        var existing = _repo.GetById(id);
        if (!_repo.Delete(id))
            return NotFound();
        if (existing is not null)
        {
            WriteAudit("delete", existing);
        }
        return NoContent();
    }

    private bool TryResolvePoiFromQr(string? code, out PoiDto poi)
    {
        poi = null!;
        var rawCode = code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return false;
        }

        if (TryParsePoiId(rawCode, out var directPoiId))
        {
            var directPoi = _repo.GetById(directPoiId);
            if (directPoi is { IsActive: true })
            {
                poi = directPoi;
                return true;
            }
        }

        var qrCode = ExtractQrCode(rawCode);
        var qrItem = _qrCodeRepository.GetActiveByCode(qrCode);
        if (qrItem is not null &&
            string.Equals(qrItem.TargetType, QrTargetKinds.Poi, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(qrItem.TargetId, out var qrPoiId))
        {
            var qrPoi = _repo.GetById(qrPoiId);
            if (qrPoi is { IsActive: true })
            {
                poi = qrPoi;
                return true;
            }
        }

        var byCode = _repo.GetAll().FirstOrDefault(item =>
            item.IsActive &&
            string.Equals(item.Code, qrCode, StringComparison.OrdinalIgnoreCase));
        if (byCode is null)
        {
            return false;
        }

        poi = byCode;
        return true;
    }

    private static bool TryParsePoiId(string rawCode, out Guid poiId)
    {
        poiId = Guid.Empty;
        if (Guid.TryParse(rawCode, out poiId))
        {
            return true;
        }

        if (!Uri.TryCreate(rawCode, UriKind.Absolute, out var uri))
        {
            return TryParsePoiPath(rawCode, out poiId);
        }

        if (string.Equals(uri.Scheme, "vinhkhanhguide", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(uri.Host, QrTargetKinds.Poi, StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return segments.Length > 0 && Guid.TryParse(segments[^1], out poiId);
        }

        return TryParsePoiPath(uri.AbsolutePath, out poiId);
    }

    private static bool TryParsePoiPath(string path, out Guid poiId)
    {
        poiId = Guid.Empty;
        var segments = path
            .Trim()
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length >= 3 &&
               string.Equals(segments[0], "qr", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[1], QrTargetKinds.Poi, StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(segments[2], out poiId);
    }

    private static string ExtractQrCode(string rawCode)
    {
        var path = rawCode.Trim();
        if (Uri.TryCreate(rawCode, UriKind.Absolute, out var uri) &&
            !string.Equals(uri.Scheme, "vinhkhanhguide", StringComparison.OrdinalIgnoreCase))
        {
            path = uri.AbsolutePath;
        }

        var segments = path
            .Trim()
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length >= 2 &&
               string.Equals(segments[0], "qr", StringComparison.OrdinalIgnoreCase)
            ? segments[1]
            : rawCode;
    }

    private void WriteAudit(string action, PoiDto poi)
    {
        _auditLogRepository.Create(new AuditLogCreateRequest
        {
            Username = User.Identity?.Name ?? "WebAdmin",
            Action = action,
            EntityName = "POI",
            EntityId = poi.Id.ToString(),
            Description = $"{action} POI {poi.Code} - {poi.Name}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });
    }
}

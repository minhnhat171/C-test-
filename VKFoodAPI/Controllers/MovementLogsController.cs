using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/movement-logs")]
public sealed class MovementLogsController : ControllerBase
{
    private readonly MovementLogRepository _repository;

    public MovementLogsController(MovementLogRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<MovementLogDto>> GetLogs(
        [FromQuery] string? userCode = null,
        [FromQuery] string? deviceId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] int? limit = null)
    {
        return Ok(_repository.GetLogs(userCode, deviceId, fromUtc, toUtc, limit));
    }

    [HttpPost]
    public ActionResult<MovementLogDto> Create([FromBody] MovementLogCreateRequest request)
    {
        try
        {
            var created = _repository.Create(request);
            return CreatedAtAction(nameof(GetLogs), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

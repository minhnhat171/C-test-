using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/analytics/listening-history")]
public class ListeningHistoryController : ControllerBase
{
    private readonly ListeningHistoryRepository _repository;

    public ListeningHistoryController(ListeningHistoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ListeningHistoryEntryDto>> GetListeningHistory(
        [FromQuery] string? sortBy = null,
        [FromQuery] string? period = null,
        [FromQuery] string? userCode = null,
        [FromQuery] string? userEmail = null,
        [FromQuery] int? limit = null)
    {
        return Ok(_repository.GetListeningHistory(period, sortBy, userCode, userEmail, limit));
    }

    [HttpGet("ranking")]
    public ActionResult<IEnumerable<PoiListeningCountDto>> CountListeningByPoi(
        [FromQuery] string? period = null,
        [FromQuery] string? userCode = null,
        [FromQuery] string? userEmail = null)
    {
        return Ok(_repository.CountListeningByPoi(period, userCode, userEmail));
    }

    [HttpPost]
    public ActionResult<ListeningHistoryEntryDto> Create([FromBody] ListeningHistoryCreateRequest request)
    {
        var created = _repository.Create(request);
        return CreatedAtAction(nameof(GetListeningHistory), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] ListeningHistoryUpdateRequest request)
    {
        return _repository.Update(id, request)
            ? NoContent()
            : NotFound();
    }
}

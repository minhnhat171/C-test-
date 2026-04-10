using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoisController : ControllerBase
{
    private readonly PoiRepository _repo;

    public PoisController(PoiRepository repo)
    {
        _repo = repo;
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

    [HttpPost]
    public ActionResult<PoiDto> Create([FromBody] PoiDto dto)
    {
        var created = _repo.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] PoiDto dto)
    {
        if (!_repo.Update(id, dto))
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (!_repo.Delete(id))
            return NotFound();
        return NoContent();
    }
}

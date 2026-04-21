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
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public ActionResult<PoiDto> Create([FromBody] PoiDto dto)
    {
        try
        {
            var created = _repo.Create(dto);
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
        if (!_repo.Delete(id))
            return NotFound();
        return NoContent();
    }
}

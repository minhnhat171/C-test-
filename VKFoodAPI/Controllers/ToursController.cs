using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly TourRepository _repo;

    public ToursController(TourRepository repo)
    {
        _repo = repo;
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
    public ActionResult<TourDto> Create([FromBody] TourDto dto)
    {
        if (dto.PoiIds is null || dto.PoiIds.Count == 0)
        {
            ModelState.AddModelError(nameof(TourDto.PoiIds), "Tour phải có ít nhất một POI.");
            return ValidationProblem(ModelState);
        }

        var created = _repo.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] TourDto dto)
    {
        if (dto.PoiIds is null || dto.PoiIds.Count == 0)
        {
            ModelState.AddModelError(nameof(TourDto.PoiIds), "Tour phải có ít nhất một POI.");
            return ValidationProblem(ModelState);
        }

        if (!_repo.Update(id, dto))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if (!_repo.Delete(id))
        {
            return NotFound();
        }

        return NoContent();
    }
}

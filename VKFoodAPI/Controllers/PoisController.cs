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
        return Ok(_repo.Pois);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PoiDto> GetById(Guid id)
    {
        var poi = _repo.Pois.FirstOrDefault(x => x.Id == id);
        if (poi == null)
            return NotFound();

        return Ok(poi);
    }

    [HttpPost]
    public ActionResult<PoiDto> Create([FromBody] PoiDto dto)
    {
        dto.Id = Guid.NewGuid();
        _repo.Pois.Add(dto);

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] PoiDto dto)
    {
        var poi = _repo.Pois.FirstOrDefault(x => x.Id == id);
        if (poi == null)
            return NotFound();

        poi.Code = dto.Code;
        poi.Name = dto.Name;
        poi.Category = dto.Category;
        poi.ImageSource = dto.ImageSource;
        poi.Address = dto.Address;
        poi.Description = dto.Description;
        poi.SpecialDish = dto.SpecialDish;
        poi.NarrationText = dto.NarrationText;
        poi.MapLink = dto.MapLink;
        poi.AudioAssetPath = dto.AudioAssetPath;
        poi.Priority = dto.Priority;
        poi.Latitude = dto.Latitude;
        poi.Longitude = dto.Longitude;
        poi.TriggerRadiusMeters = dto.TriggerRadiusMeters;
        poi.CooldownMinutes = dto.CooldownMinutes;
        poi.IsActive = dto.IsActive;
        poi.NarrationTranslations = dto.NarrationTranslations ?? new();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var poi = _repo.Pois.FirstOrDefault(x => x.Id == id);
        if (poi == null)
            return NotFound();

        _repo.Pois.Remove(poi);
        return NoContent();
    }
}

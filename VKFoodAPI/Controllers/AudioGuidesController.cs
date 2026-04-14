using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioGuidesController : ControllerBase
{
    private readonly AudioGuideRepository _repo;

    public AudioGuidesController(AudioGuideRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public ActionResult<IEnumerable<AudioGuideDto>> GetAll()
    {
        return Ok(_repo.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<AudioGuideDto> GetById(Guid id)
    {
        var audioGuide = _repo.GetById(id);
        return audioGuide is null ? NotFound() : Ok(audioGuide);
    }

    [HttpPost]
    public ActionResult<AudioGuideDto> Create([FromBody] AudioGuideDto dto)
    {
        var validationProblem = ValidateRequest(dto);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var created = _repo.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] AudioGuideDto dto)
    {
        var validationProblem = ValidateRequest(dto);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        return _repo.Update(id, dto)
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _repo.Delete(id)
            ? NoContent()
            : NotFound();
    }

    private ActionResult? ValidateRequest(AudioGuideDto dto)
    {
        if (dto.PoiId == Guid.Empty)
        {
            return BadRequest("PoiId is required.");
        }

        if (!_repo.PoiExists(dto.PoiId))
        {
            return BadRequest("The referenced POI does not exist.");
        }

        if (string.Equals(dto.SourceType?.Trim(), "file", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(dto.FilePath))
            {
                return BadRequest("FilePath is required when SourceType is file.");
            }
        }
        else if (string.IsNullOrWhiteSpace(dto.Script))
        {
            return BadRequest("Script is required when SourceType is tts.");
        }

        return null;
    }
}

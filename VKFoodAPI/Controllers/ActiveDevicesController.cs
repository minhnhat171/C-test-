using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/analytics/active-devices")]
public class ActiveDevicesController : ControllerBase
{
    private readonly ActiveDeviceRepository _repository;

    public ActiveDevicesController(ActiveDeviceRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<ActiveDeviceStatsDto> GetStats()
    {
        return Ok(_repository.GetStats());
    }

    [HttpPost("heartbeat")]
    public ActionResult<ActiveDeviceStatsDto> Heartbeat([FromBody] ActiveDeviceHeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        return Ok(_repository.RegisterHeartbeat(request));
    }

    [HttpPost("disconnect")]
    public ActionResult<ActiveDeviceStatsDto> Disconnect([FromBody] ActiveDeviceDisconnectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        return Ok(_repository.Disconnect(request));
    }
}

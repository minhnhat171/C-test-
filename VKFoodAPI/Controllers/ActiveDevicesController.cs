using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/analytics/active-devices")]
[Route("api/device-presence")]
public class ActiveDevicesController : ControllerBase
{
    private readonly ActiveDeviceRepository _repository;
    private readonly MovementLogRepository _movementLogRepository;

    public ActiveDevicesController(
        ActiveDeviceRepository repository,
        MovementLogRepository movementLogRepository)
    {
        _repository = repository;
        _movementLogRepository = movementLogRepository;
    }

    [HttpGet]
    public ActionResult<ActiveDeviceStatsDto> GetStats()
    {
        return Ok(_repository.GetStats());
    }

    [HttpGet("raw")]
    [Authorize(Policy = AdminApiKeyDefaults.PolicyName)]
    public ActionResult<IEnumerable<ActiveDeviceSessionDto>> GetRawSessions()
    {
        return Ok(_repository.GetRawSessions());
    }

    [HttpPost("heartbeat")]
    public ActionResult<ActiveDeviceStatsDto> Heartbeat([FromBody] ActiveDeviceHeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        var stats = _repository.RegisterHeartbeat(request);
        TryWriteMovementLog(request);
        return Ok(stats);
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

    private void TryWriteMovementLog(ActiveDeviceHeartbeatRequest request)
    {
        if (!request.Latitude.HasValue ||
            !request.Longitude.HasValue ||
            request.Latitude.Value is < -90 or > 90 ||
            request.Longitude.Value is < -180 or > 180 ||
            (Math.Abs(request.Latitude.Value) <= 0.000001 &&
             Math.Abs(request.Longitude.Value) <= 0.000001))
        {
            return;
        }

        _movementLogRepository.Create(new MovementLogCreateRequest
        {
            DeviceId = request.DeviceId,
            UserCode = request.UserCode,
            UserDisplayName = request.UserDisplayName,
            UserEmail = request.UserEmail,
            DevicePlatform = request.DevicePlatform,
            DeviceModel = request.DeviceModel,
            AppVersion = request.AppVersion,
            Latitude = request.Latitude.Value,
            Longitude = request.Longitude.Value,
            AccuracyMeters = request.AccuracyMeters,
            RecordedAtUtc = request.LocationTimestampUtc ?? request.SentAtUtc
        });
    }
}

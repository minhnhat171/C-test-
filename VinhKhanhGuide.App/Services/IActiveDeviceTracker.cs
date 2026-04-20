namespace VinhKhanhGuide.App.Services;

public interface IActiveDeviceTracker
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendHeartbeatAsync(CancellationToken cancellationToken = default);
}

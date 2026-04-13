using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface ILocationService
{
    event EventHandler<LocationDto>? LocationUpdated;

    Task<bool> EnsurePermissionAsync(bool requestIfNeeded = true);
    Task<LocationDto?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
    Task StartListeningAsync();
    Task StopListeningAsync();
}

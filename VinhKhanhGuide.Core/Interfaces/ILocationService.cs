using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface ILocationService
{
    event EventHandler<LocationDto>? LocationUpdated;

    Task StartListeningAsync();
    Task StopListeningAsync();
}

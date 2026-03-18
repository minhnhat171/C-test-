using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

public class MapViewModel
{
    private readonly ILocationService _locationService;

    public MapViewModel(ILocationService locationService)
    {
        _locationService = locationService;
        _locationService.LocationUpdated += OnLocationUpdated;
    }

    private void OnLocationUpdated(object? sender, LocationDto location)
    {
        Console.WriteLine($"{location.Latitude}, {location.Longitude}");
    }
}
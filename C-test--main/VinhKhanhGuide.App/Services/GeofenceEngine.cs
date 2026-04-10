using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class GeofenceEngine
{
    public IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> Evaluate(LocationDto location, IReadOnlyList<POI> pois)
    {
        var results = pois
            .Select(poi =>
            {
                var distance = GeoMath.DistanceMeters(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);
                return (Poi: poi, DistanceMeters: distance, IsInside: distance <= poi.TriggerRadiusMeters);
            })
            .OrderBy(r => r.DistanceMeters)
            .ThenByDescending(r => r.Poi.Priority)
            .ToList();

        return results;
    }
}

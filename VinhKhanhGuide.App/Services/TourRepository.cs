using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public sealed class TourRepository : ITourRepository
{
    private readonly ITourProvider _tourProvider;
    private readonly object _syncRoot = new();
    private IReadOnlyList<TourDto> _cachedTours = Array.Empty<TourDto>();

    public TourRepository(ITourProvider tourProvider)
    {
        _tourProvider = tourProvider;
    }

    public async Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        var tours = await _tourProvider.GetToursAsync(cancellationToken);
        StoreSnapshot(tours);
        return CloneTours(tours);
    }

    public async Task<TourDto?> GetTourByIdAsync(int tourId, CancellationToken cancellationToken = default)
    {
        if (tourId <= 0)
        {
            return null;
        }

        var cachedTour = GetCachedTours().FirstOrDefault(item => item.Id == tourId);
        if (cachedTour is not null)
        {
            return cachedTour.Clone();
        }

        var tour = await _tourProvider.GetTourByIdAsync(tourId, cancellationToken);
        if (tour is null)
        {
            return null;
        }

        MergeTourIntoSnapshot(tour);
        return tour.Clone();
    }

    public IReadOnlyList<TourDto> GetCachedTours()
    {
        lock (_syncRoot)
        {
            return CloneTours(_cachedTours);
        }
    }

    public void StoreSnapshot(IEnumerable<TourDto> tours)
    {
        lock (_syncRoot)
        {
            _cachedTours = CloneTours(tours);
        }
    }

    private void MergeTourIntoSnapshot(TourDto tour)
    {
        lock (_syncRoot)
        {
            var nextSnapshot = _cachedTours.ToList();
            var existingIndex = nextSnapshot.FindIndex(item => item.Id == tour.Id);

            if (existingIndex >= 0)
            {
                nextSnapshot[existingIndex] = tour.Clone();
            }
            else
            {
                nextSnapshot.Add(tour.Clone());
            }

            _cachedTours = nextSnapshot;
        }
    }

    private static IReadOnlyList<TourDto> CloneTours(IEnumerable<TourDto> tours)
    {
        return tours
            .Select(item => item.Clone())
            .ToList();
    }
}

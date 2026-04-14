using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public interface ITourRepository
{
    Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default);

    Task<TourDto?> GetTourByIdAsync(int tourId, CancellationToken cancellationToken = default);

    IReadOnlyList<TourDto> GetCachedTours();

    void StoreSnapshot(IEnumerable<TourDto> tours);
}

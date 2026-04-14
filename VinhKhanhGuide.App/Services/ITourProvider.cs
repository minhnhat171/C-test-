using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public interface ITourProvider
{
    Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default);

    Task<TourDto?> GetTourByIdAsync(int tourId, CancellationToken cancellationToken = default);
}

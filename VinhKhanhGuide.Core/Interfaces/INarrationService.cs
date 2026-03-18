using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface INarrationService
{
    Task NarrateAsync(POI poi, CancellationToken cancellationToken = default);
}

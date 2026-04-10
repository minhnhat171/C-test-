using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface INarrationService
{
    Task NarrateAsync(POI poi, string? languageCode = null, CancellationToken cancellationToken = default);

    // Giữ thêm hàm này để không vỡ code cũ nếu đang gọi SpeakAsync ở đâu đó
    Task SpeakAsync(string text, string? languageCode = null, CancellationToken cancellationToken = default);

    Task StopAsync();
}

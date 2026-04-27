using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public interface IQrResolveService
{
    Task<ResolveQrResponseDto?> ResolveAsync(string code, CancellationToken cancellationToken = default);
}

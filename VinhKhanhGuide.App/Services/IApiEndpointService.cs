namespace VinhKhanhGuide.App.Services;

public interface IApiEndpointService
{
    Uri CurrentBaseUri { get; }
    bool TrySetBaseUrl(string? baseUrl);
}

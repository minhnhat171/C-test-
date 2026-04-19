namespace VinhKhanhGuide.App.Services;

internal static class PoiApiEndpoint
{
    private const string PublicApiBaseUrl = "https://negotiate-abide-remover.ngrok-free.dev/";

    public static Uri CreateBaseUri()
    {
        return new Uri(PublicApiBaseUrl);
    }
}

namespace VinhKhanhGuide.App.Services;

internal static class PoiApiEndpoint
{
    public static Uri CreateBaseUri()
    {
#if ANDROID
        return new Uri("http://10.0.2.2:5287/");
#else
        return new Uri("http://localhost:5287/");
#endif
    }
}

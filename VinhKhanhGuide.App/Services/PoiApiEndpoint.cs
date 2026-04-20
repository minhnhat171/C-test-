using VinhKhanhGuide.Core.Configuration;

namespace VinhKhanhGuide.App.Services;

internal static class PoiApiEndpoint
{
    public static Uri CreateBaseUri()
    {
#if DEBUG && ANDROID
        return PoiApiDefaults.CreateBaseUri(PoiApiDefaults.AndroidEmulatorBaseUrl);
#elif DEBUG
        return PoiApiDefaults.CreateBaseUri(PoiApiDefaults.LocalBaseUrl);
#else
        return PoiApiDefaults.CreateBaseUri(PoiApiDefaults.PublicBaseUrl);
#endif
    }
}

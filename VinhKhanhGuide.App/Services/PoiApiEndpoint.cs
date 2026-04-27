using VinhKhanhGuide.Core.Configuration;

namespace VinhKhanhGuide.App.Services;

internal static class PoiApiEndpoint
{
    public static Uri CreateBaseUri()
    {
        return PoiApiDefaults.CreateBaseUri(AppBuildConfiguration.ApiBaseUrl);
    }
}

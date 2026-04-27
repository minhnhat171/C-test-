using VinhKhanhGuide.Core.Configuration;

namespace VinhKhanhGuide.App.Services;

internal static class PoiApiEndpoint
{
    public static Uri CreateBuildBaseUri()
    {
        return PoiApiDefaults.CreateBaseUri(AppBuildConfiguration.ApiBaseUrl);
    }
}

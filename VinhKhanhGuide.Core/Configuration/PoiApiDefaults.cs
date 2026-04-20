namespace VinhKhanhGuide.Core.Configuration;

public static class PoiApiDefaults
{
    public const string LocalBaseUrl = "http://localhost:5287/";
    public const string AndroidEmulatorBaseUrl = "http://10.0.2.2:5287/";
    public const string PublicBaseUrl = LocalBaseUrl;

    public static Uri CreateBaseUri(string? configuredBaseUrl = null)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? LocalBaseUrl
            : configuredBaseUrl.Trim();

        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }

        return new Uri(baseUrl, UriKind.Absolute);
    }
}

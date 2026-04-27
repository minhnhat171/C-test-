using Microsoft.Maui.Storage;

namespace VinhKhanhGuide.App.Services;

public sealed class ApiEndpointService : IApiEndpointService
{
    private const string PreferenceKey = "vinhkhanh.api.base-url.v1";
    private readonly object _syncRoot = new();
    private Uri _currentBaseUri;

    public ApiEndpointService()
    {
        _currentBaseUri = CreateBaseUri(Preferences.Default.Get(PreferenceKey, string.Empty))
            ?? PoiApiEndpoint.CreateBuildBaseUri();
    }

    public Uri CurrentBaseUri
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentBaseUri;
            }
        }
    }

    public bool TrySetBaseUrl(string? baseUrl)
    {
        var nextBaseUri = CreateBaseUri(baseUrl);
        if (nextBaseUri is null)
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (Uri.Compare(
                    _currentBaseUri,
                    nextBaseUri,
                    UriComponents.SchemeAndServer | UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return false;
            }

            _currentBaseUri = nextBaseUri;
            Preferences.Default.Set(PreferenceKey, nextBaseUri.ToString());
            return true;
        }
    }

    private static Uri? CreateBaseUri(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var value = baseUrl.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!value.EndsWith("/", StringComparison.Ordinal))
        {
            value += "/";
        }

        return new Uri(value, UriKind.Absolute);
    }
}

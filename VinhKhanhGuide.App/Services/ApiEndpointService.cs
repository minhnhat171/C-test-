using System.Net;
using Microsoft.Maui.Storage;

namespace VinhKhanhGuide.App.Services;

public sealed class ApiEndpointService : IApiEndpointService
{
    private const string PreferenceKey = "vinhkhanh.api.base-url.v1";
    private readonly object _syncRoot = new();
    private readonly Uri _buildBaseUri;
    private Uri _currentBaseUri;

    public ApiEndpointService()
    {
        _buildBaseUri = PoiApiEndpoint.CreateBuildBaseUri();
        var storedBaseUri = CreateBaseUri(Preferences.Default.Get(PreferenceKey, string.Empty));

        if (storedBaseUri is not null &&
            CanUseConfiguredBaseUri(storedBaseUri, _buildBaseUri))
        {
            _currentBaseUri = storedBaseUri;
        }
        else
        {
            Preferences.Default.Remove(PreferenceKey);
            _currentBaseUri = _buildBaseUri;
        }
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

        if (!CanUseConfiguredBaseUri(nextBaseUri, _buildBaseUri))
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

    private static bool CanUseConfiguredBaseUri(Uri candidateBaseUri, Uri buildBaseUri)
    {
        return !IsLocalOrPrivateBaseUri(candidateBaseUri) ||
               IsLocalOrPrivateBaseUri(buildBaseUri);
    }

    private static bool IsLocalOrPrivateBaseUri(Uri uri)
    {
        var host = uri.Host.Trim();
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                   bytes[0] == 192 && bytes[1] == 168 ||
                   bytes[0] == 169 && bytes[1] == 254;
        }

        return address.IsIPv6LinkLocal ||
               address.IsIPv6SiteLocal ||
               address.IsIPv6UniqueLocal;
    }
}

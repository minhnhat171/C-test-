using Microsoft.Maui.Controls;

namespace VinhKhanhGuide.App.Services;

internal static class AppImageSourceResolver
{
    public static ImageSource? Resolve(string? imageSource)
    {
        var value = imageSource?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return CreateRemoteImageSource(absoluteUri);
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            return CreateRemoteImageSource(new Uri(PoiApiEndpoint.CreateBuildBaseUri(), value.TrimStart('/')));
        }

        return ImageSource.FromFile(value);
    }

    private static UriImageSource CreateRemoteImageSource(Uri uri)
    {
        return new UriImageSource
        {
            Uri = uri,
            CachingEnabled = false
        };
    }
}

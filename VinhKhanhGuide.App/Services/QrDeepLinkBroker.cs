using System.Globalization;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public sealed record QrDeepLinkRequest(
    string TargetType,
    string TargetId,
    bool AutoPlay,
    string Source,
    string ApiBaseUrl);

public static class QrDeepLinkBroker
{
    private static readonly object SyncRoot = new();
    private static QrDeepLinkRequest? _pendingRequest;

    public static event EventHandler? PendingRequestAvailable;

    public static bool Publish(string? uriText)
    {
        if (!TryParse(uriText, out var request))
        {
            return false;
        }

        lock (SyncRoot)
        {
            _pendingRequest = request;
        }

        PendingRequestAvailable?.Invoke(null, EventArgs.Empty);
        return true;
    }

    public static bool TryConsumePendingRequest(out QrDeepLinkRequest request)
    {
        lock (SyncRoot)
        {
            if (_pendingRequest is null)
            {
                request = null!;
                return false;
            }

            request = _pendingRequest;
            _pendingRequest = null;
            return true;
        }
    }

    private static bool TryParse(string? uriText, out QrDeepLinkRequest request)
    {
        request = null!;

        if (string.IsNullOrWhiteSpace(uriText) ||
            !Uri.TryCreate(uriText, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "vinhkhanhguide", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var targetType = uri.Host;
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.IsNullOrWhiteSpace(targetType) && segments.Length > 0)
        {
            targetType = segments[0];
            segments = segments.Skip(1).ToArray();
        }

        if (segments.Length == 0)
        {
            return false;
        }

        if (!QrTargetKinds.IsSupported(targetType))
        {
            return false;
        }

        targetType = QrTargetKinds.Normalize(targetType);
        var targetId = segments[^1];
        if (string.Equals(targetType, QrTargetKinds.Poi, StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(targetId, out var poiId))
            {
                return false;
            }

            targetId = poiId.ToString();
        }
        else if (string.Equals(targetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
            {
                return false;
            }

            targetId = tourId.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            return false;
        }

        var query = ParseQuery(uri.Query);
        var autoPlay = !query.TryGetValue("autoplay", out var autoPlayValue) ||
                       !string.Equals(autoPlayValue, "0", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(autoPlayValue, "false", StringComparison.OrdinalIgnoreCase);

        request = new QrDeepLinkRequest(
            targetType,
            targetId,
            autoPlay,
            query.TryGetValue("source", out var source) && !string.IsNullOrWhiteSpace(source)
                ? source
                : "qr",
            query.TryGetValue("api", out var apiBaseUrl) ? apiBaseUrl : string.Empty);

        return true;
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return values;
        }

        foreach (var pair in query.TrimStart('?')
                     .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex < 0)
            {
                values[Uri.UnescapeDataString(pair)] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separatorIndex]);
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            values[key] = value;
        }

        return values;
    }
}

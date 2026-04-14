using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace VinhKhanhGuide.App.Services;

internal static class MapTileHttpClientFactory
{
    public const string UserAgent = "VinhKhanhGuideApp/1.0";

    private static readonly Lazy<HttpClient> SharedClientValue = new(CreateHttpClient);
    private static IMapOfflineTileService? _offlineTileService;

    public static HttpClient SharedClient => SharedClientValue.Value;

    public static void ConfigureOfflineTileService(IMapOfflineTileService offlineTileService)
    {
        _offlineTileService = offlineTileService;
    }

    public static void ConfigureRequest(HttpRequestMessage request)
    {
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        request.Headers.UserAgent.Clear();
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("image/png");
        request.Headers.Accept.ParseAdd("image/*;q=0.9");
        request.Headers.Accept.ParseAdd("*/*;q=0.5");
    }

    private static HttpClient CreateHttpClient()
    {
#if ANDROID
        var socketsHandler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        var loggingHandler = new TileLoggingHandler(socketsHandler);
        var offlineCacheHandler = new TileOfflineCacheHandler(loggingHandler);

        return new HttpClient(offlineCacheHandler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
#else
        return new HttpClient();
#endif
    }

    private sealed class TileLoggingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                Debug.WriteLine($"[MapTile] {request.RequestUri} -> {(int)response.StatusCode}");
                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapTile] {request.RequestUri} failed: {ex}");
                throw;
            }
        }
    }

    private sealed class TileOfflineCacheHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var offlineTileService = _offlineTileService;
            if (offlineTileService is not null)
            {
                var cachedTile = await offlineTileService.TryGetCachedTileAsync(request.RequestUri, cancellationToken);
                if (cachedTile is not null)
                {
                    var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        RequestMessage = request,
                        Content = new ByteArrayContent(cachedTile)
                    };
                    cachedResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    return cachedResponse;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}

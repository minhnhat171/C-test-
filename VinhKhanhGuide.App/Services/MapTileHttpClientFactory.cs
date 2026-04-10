using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace VinhKhanhGuide.App.Services;

internal static class MapTileHttpClientFactory
{
    public const string UserAgent = "VinhKhanhGuideApp/1.0";

    private static readonly Lazy<HttpClient> SharedClientValue = new(CreateHttpClient);

    public static HttpClient SharedClient => SharedClientValue.Value;

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

        return new HttpClient(loggingHandler, disposeHandler: true)
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
}

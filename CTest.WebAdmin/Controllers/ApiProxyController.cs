using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Configuration;

namespace CTest.WebAdmin.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/{**path}")]
public sealed class ApiProxyController : ControllerBase
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _apiBaseUri;

    public ApiProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("PublicApiProxy");
        _apiBaseUri = PoiApiDefaults.CreateBaseUri(configuration["PoiApi:BaseUrl"]);
    }

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS")]
    public async Task<IActionResult> Proxy(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotFound();
        }

        var targetUri = BuildTargetUri(path);
        using var requestMessage = new HttpRequestMessage(
            new HttpMethod(Request.Method),
            targetUri);

        CopyRequestHeaders(requestMessage);

        if (Request.ContentLength is > 0 ||
            Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            requestMessage.Content = new StreamContent(Request.Body);
            foreach (var header in Request.Headers)
            {
                if (!string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                requestMessage.Content.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value.ToArray());
            }
        }

        using var responseMessage = await _httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        Response.StatusCode = (int)responseMessage.StatusCode;
        CopyResponseHeaders(responseMessage);
        await responseMessage.Content.CopyToAsync(Response.Body, cancellationToken);

        return new EmptyResult();
    }

    private Uri BuildTargetUri(string path)
    {
        var escapedPath = string.Join(
            "/",
            path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        var relative = $"api/{escapedPath}{Request.QueryString}";
        return new Uri(_apiBaseUri, relative);
    }

    private void CopyRequestHeaders(HttpRequestMessage requestMessage)
    {
        foreach (var header in Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) ||
                string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private void CopyResponseHeaders(HttpResponseMessage responseMessage)
    {
        foreach (var header in responseMessage.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        Response.Headers.Remove("transfer-encoding");
    }
}

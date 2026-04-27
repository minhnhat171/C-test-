namespace VinhKhanhGuide.App.Services;

public sealed class ApiEndpointMessageHandler : DelegatingHandler
{
    public static readonly Uri PlaceholderBaseUri = new("https://app-api.local/");

    private readonly IApiEndpointService _endpointService;

    public ApiEndpointMessageHandler(
        IApiEndpointService endpointService,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _endpointService = endpointService;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is { IsAbsoluteUri: true } requestUri &&
            string.Equals(requestUri.Scheme, PlaceholderBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(requestUri.Host, PlaceholderBaseUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            request.RequestUri = new Uri(
                _endpointService.CurrentBaseUri,
                requestUri.PathAndQuery.TrimStart('/'));
        }

        if (request.RequestUri?.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase) == true)
        {
            request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
        }

        return base.SendAsync(request, cancellationToken);
    }
}

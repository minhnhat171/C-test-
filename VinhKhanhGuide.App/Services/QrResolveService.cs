using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public sealed class QrResolveService : IQrResolveService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QrResolveService> _logger;

    public QrResolveService(
        HttpClient httpClient,
        ILogger<QrResolveService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ResolveQrResponseDto?> ResolveAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        try
        {
            var encodedCode = Uri.EscapeDataString(code.Trim());
            var response = await _httpClient.GetAsync($"api/resolve-qr?code={encodedCode}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ResolveQrResponseDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve QR code {Code}.", code);
            return null;
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class ListeningHistorySyncService : IListeningHistorySyncService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILogger<ListeningHistorySyncService> _logger;

    public ListeningHistorySyncService(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<ListeningHistorySyncService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    public async Task<Guid?> BeginAsync(
        POI poi,
        string? language,
        bool autoTriggered,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = _authService.CurrentSession;
            var request = new ListeningHistoryCreateRequest
            {
                PoiId = poi.Id,
                PoiCode = poi.Code,
                PoiName = poi.Name,
                UserCode = session?.Email ?? "guest",
                UserDisplayName = session?.FullName ?? "Khach",
                UserEmail = session?.Email ?? string.Empty,
                TriggerType = autoTriggered ? "GPS" : "APP",
                Language = string.IsNullOrWhiteSpace(language) ? "vi" : language!.Trim(),
                Source = "app",
                DevicePlatform = DeviceInfo.Current.Platform.ToString(),
                AutoTriggered = autoTriggered,
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/analytics/listening-history",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<ListeningHistoryEntryDto>(
                cancellationToken: cancellationToken);

            return created?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create listening history entry for POI {PoiId}.", poi.Id);
            return null;
        }
    }

    public async Task CompleteAsync(
        Guid historyId,
        int listenSeconds,
        bool completed,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (historyId == Guid.Empty)
        {
            return;
        }

        try
        {
            var request = new ListeningHistoryUpdateRequest
            {
                ListenSeconds = Math.Max(0, listenSeconds),
                Completed = completed,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage?.Trim() ?? string.Empty
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"api/analytics/listening-history/{historyId}",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update listening history entry {HistoryId}.", historyId);
        }
    }
}
